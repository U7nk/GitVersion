using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NSubstitute;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class RepositoryExtensionsTests : TestBase
    {
        private static void EnsureLocalBranchExistsForCurrentBranch(IGitRepository repo, ILog log, Remote remote, string currentBranch)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (remote is null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (string.IsNullOrEmpty(currentBranch)) return;

            var isRef = currentBranch.Contains("refs");
            var isBranch = currentBranch.Contains("refs/heads");
            var localCanonicalName = !isRef
                ? "refs/heads/" + currentBranch
                : isBranch
                    ? currentBranch
                    : currentBranch.Replace("refs/", "refs/heads/");

            var repoTip = repo.Head.Tip;

            // We currently have the rep.Head of the *default* branch, now we need to look up the right one
            var originCanonicalName = $"{remote.Name}/{currentBranch}";
            var originBranch = repo.Branches[originCanonicalName];
            if (originBranch != null)
            {
                repoTip = originBranch.Tip;
            }

            var repoTipId = repoTip.Id;

            if (repo.Branches.All(b => !b.CanonicalName.IsEquivalentTo(localCanonicalName)))
            {
                log.Info(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                log.Info(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                var localRef = repo.Refs[localCanonicalName];
                repo.Refs.UpdateTarget(localRef, repoTipId);
            }

            repo.Commands.Checkout(localCanonicalName);
        }

        [Test]
        public void EnsureLocalBranchExistsForCurrentBranch_CaseInsensitivelyMatchesBranches()
        {
            var log = Substitute.For<ILog>();
            var repository = MockRepository();
            var remote = MockRemote(repository);

            EnsureLocalBranchExistsForCurrentBranch(repository, log, remote, "refs/heads/featurE/feat-test");
        }

        private IGitRepository MockRepository()
        {
            var repository = Substitute.For<IGitRepository>();
            var commands = Substitute.For<IGitRepositoryCommands>();
            repository.Commands.Returns(commands);
            return repository;
        }

        private Remote MockRemote(IGitRepository repository)
        {
            var branches = new TestableBranchCollection();
            var tipId = new ObjectId("c6d8764d20ff16c0df14c73680e52b255b608926");
            var tip = new TestableCommit(tipId);
            var head = branches.Add("refs/heads/feature/feat-test", tip);
            var remote = new TesatbleRemote("origin");
            var references = new TestableReferenceCollection();
            _ = references.Add("develop", "refs/heads/develop");

            repository.Refs.Returns(references);
            repository.Head.Returns(head);
            repository.Branches.Returns(branches);
            return remote;
        }

        private class TestableBranchCollection : BranchCollection
        {
            IDictionary<string, Branch> branches = new Dictionary<string, Branch>();

            public override Branch this[string name] =>
                this.branches.ContainsKey(name)
                    ? this.branches[name]
                    : null;

            public override Branch Add(string name, Commit commit)
            {
                var branch = new TestableBranch(name, commit);
                this.branches.Add(name, branch);
                return branch;
            }

            public override Branch Add(string name, string committish)
            {
                var id = new ObjectId(committish);
                var commit = new TestableCommit(id);
                return Add(name, commit);
            }

            public override Branch Add(string name, Commit commit, bool allowOverwrite)
            {
                return Add(name, commit);
            }

            public override Branch Add(string name, string committish, bool allowOverwrite)
            {
                return Add(name, committish);
            }

            public override IEnumerator<Branch> GetEnumerator()
            {
                return this.branches.Values.GetEnumerator();
            }

            public override void Remove(string name)
            {
                this.branches.Remove(name);
            }

            public override void Remove(string name, bool isRemote)
            {
                this.branches.Remove(name);
            }

            public override void Remove(Branch branch)
            {
                this.branches.Remove(branch.CanonicalName);
            }

            public override Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
            {
                return base.Update(branch, actions);
            }
        }

        private class TestableBranch : Branch
        {
            private readonly string canonicalName;
            private readonly Commit tip;

            public TestableBranch(string canonicalName, Commit tip)
            {
                this.tip = tip;
                this.canonicalName = canonicalName;
            }

            public override string CanonicalName => this.canonicalName;
            public override Commit Tip => this.tip;
        }

        private class TestableCommit : Commit
        {
            private ObjectId id;

            public TestableCommit(ObjectId id)
            {
                this.id = id;
            }

            public override ObjectId Id => this.id;
        }

        private class TesatbleRemote : Remote
        {
            private string name;

            public TesatbleRemote(string name)
            {
                this.name = name;
            }

            public override string Name => this.name;
        }

        private class TestableReferenceCollection : ReferenceCollection
        {
            Reference reference;

            public override DirectReference Add(string name, ObjectId targetId)
            {
                throw new InvalidOperationException("Update should be invoked when case-insensitively comparing branches.");
            }

            public override Reference Add(string name, string canonicalRefNameOrObjectish)
            {
                return this.reference = new TestableReference(canonicalRefNameOrObjectish);
            }

            public override Reference UpdateTarget(Reference directRef, ObjectId targetId)
            {
                return this.reference;
            }

            public override Reference this[string name] => this.reference;
        }

        private class TestableReference : Reference
        {
            private readonly string canonicalName;

            public TestableReference(string canonicalName)
            {
                this.canonicalName = canonicalName;
            }

            public override string CanonicalName => this.canonicalName;

            public override DirectReference ResolveToDirectReference()
            {
                throw new NotImplementedException();
            }
        }
    }
}