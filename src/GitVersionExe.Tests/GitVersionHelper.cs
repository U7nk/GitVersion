using System;
using System.IO;
using System.Text;
using GitVersion.Helpers;
using GitVersionCore.Tests.Helpers;

namespace GitVersionExe.Tests
{
    public static class GitVersionHelper
    {
        public static ExecutionResults ExecuteIn(string workingDirectory,
            string exec = null,
            string execArgs = null,
            string projectFile = null,
            string projectArgs = null,
            bool isTeamCity = false,
            bool logToFile = true)
        {
            var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
            var args = new ArgumentBuilder(workingDirectory, exec, execArgs, projectFile, projectArgs, logFile, isTeamCity);
            return ExecuteIn(args);
        }

        public static ExecutionResults ExecuteIn(string workingDirectory, string arguments, bool isTeamCity = false, bool logToFile = true)
        {
            var logFile = logToFile ? Path.Combine(workingDirectory, "log.txt") : null;
            var args = new ArgumentBuilder(workingDirectory, arguments, isTeamCity, logFile);
            return ExecuteIn(args);
        }

        private static ExecutionResults ExecuteIn(ArgumentBuilder arguments)
        {
            var executable = PathHelper.GetExecutable();
            var output = new StringBuilder();

            var exitCode = -1;

            try
            {
                var args = PathHelper.GetExecutableArgs(arguments.ToString());

                Console.WriteLine("Executing: {0} {1}", executable, args);
                Console.WriteLine();

                exitCode = ProcessHelper.Run(
                    s => output.AppendLine(s),
                    s => output.AppendLine(s),
                    null,
                    executable,
                    args,
                    arguments.WorkingDirectory);
            }
            catch (Exception exception)
            {
                // NOTE: It's the exit code and output from the process we want to test,
                //       not the internals of the ProcessHelper. That's why we're catching
                //       any exceptions here, because problems in the process being executed
                //       should be visible in the output or exit code. @asbjornu
                Console.WriteLine(exception);
            }

            Console.WriteLine("Output from GitVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(output.ToString());
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");

            if (string.IsNullOrWhiteSpace(arguments.LogFile) || !File.Exists(arguments.LogFile))
            {
                return new ExecutionResults(exitCode, output.ToString(), null);
            }

            var logContents = File.ReadAllText(arguments.LogFile);
            Console.WriteLine("Log from GitVersion.exe");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(logContents);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------");

            return new ExecutionResults(exitCode, output.ToString(), logContents);
        }
    }
}
