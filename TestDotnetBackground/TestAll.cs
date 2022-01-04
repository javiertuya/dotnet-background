using DotnetBackground;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace TestDotnetBackground
{
    public class TestAll
    {
        [Test]
        [TestCase("run --project proj/proj.csproj", "proj", ".", "run --project proj/proj.csproj")]
        [TestCase("run --project proj/proj.name.csproj", "proj.name", ".", "run --project proj/proj.name.csproj")]
        //custom project name
        [TestCase("run --name custom --project proj/proj.csproj", "custom", ".", "run --project proj/proj.csproj")]
        [TestCase("run --project proj/proj.csproj --name custom", "custom", ".", "run --project proj/proj.csproj")]
        //output specification
        [TestCase("run --name custom --out report --project proj/proj.csproj", "custom", "report", "run --project proj/proj.csproj")]
        [TestCase("run --name custom --project proj/proj.csproj --out report", "custom", "report", "run --project proj/proj.csproj")]
        //more dotnet parameters and arguments
        [TestCase("run --name custom --out report --project proj/proj.csproj --no-restore abc def", "custom", "report", "run --project proj/proj.csproj --no-restore abc def")]
        [TestCase("run --project proj/proj.csproj --no-restore abc def --name custom --out report", "custom", "report", "run --project proj/proj.csproj --no-restore abc def")]
        public void TestArgListValid(string actArgs, string expName, string expOut, string expArgs)
        {
            string[] args = actArgs.Split(' ');
            CustomArguments arguments = new CustomArguments();
            args = arguments.Parse(args);
            Assert.AreEqual(expName, arguments.Name, "Process name");
            Assert.AreEqual(expOut, arguments.OutDir, "Output directory");
            Assert.AreEqual(expArgs, String.Join(" ",args), "Resulting command");
        }


        [Test]
        [TestCase("run --project proj/proj.csproj --out", "Parameter --out requires a value")]
        [TestCase("run --out --project proj/proj.csproj", "Parameter --out requires a value but starts with --")]
        [TestCase("run --project proj/proj.csproj --name", "Parameter --name requires a value")]
        [TestCase("run --name --project proj/proj.csproj", "Parameter --name requires a value but starts with --")]
        public void TestArgListInvalid(string actArgs, string expMessage)
        {
            var exception = Assert.Throws<CustomArgumentException>(() => new CustomArguments().Parse(actArgs.Split(' ')));
            Assert.AreEqual(expMessage, exception.Message);
        }


        private const string OutFolder = "../../../../reports";
        private const string ProcessPidLog = "../../../../reports/" + ProcessLauncher.PidFileName;
        private const string Process1OutLog = "../../../../reports/dotnet-TestProcess-out.log";
        private const string Process1ErrLog = "../../../../reports/dotnet-TestProcess-err.log";
        private const string Process2OutLog = "../../../../reports/dotnet-CustomNamed-out.log";
        private const string Process2ErrLog = "../../../../reports/dotnet-CustomNamed-err.log";
        private const string ProcessxOutLog = "../../../../reports/dotnet-ExitedProcess-out.log";
        private const string ProcessxErrLog = "../../../../reports/dotnet-ExitedProcess-err.log";

        [Test]
        public void TestWorkflow()
        {
            //Requires TestProcess project previously built.
            //setup clean environment
            Directory.CreateDirectory(OutFolder);
            File.Delete(ProcessPidLog);
            File.Delete(Process1OutLog);
            File.Delete(Process1ErrLog);
            File.Delete(Process2ErrLog);
            File.Delete(Process2OutLog);
            File.Delete(ProcessxErrLog);
            File.Delete(ProcessxOutLog);

            //Launch and wait
            ProcessLauncher launcher = new ProcessLauncher();
            launcher.DotnetRun("run --out ../../../../reports --project ../../../../TestProcess/TestProcess.csproj --no-restore".Split(" "));
            launcher.DotnetRun("run --name CustomNamed --out ../../../../reports --project ../../../../TestProcess/TestProcess.csproj --no-restore ab cd".Split(" "));
            launcher.DotnetRun("run --name ExitedProcess --out ../../../../reports --project ../../../../TestProcess/TestProcess.csproj --no-restore exit".Split(" "));
            System.Threading.Thread.Sleep(4500); //time to write some additional line

            //Read proceses pids
            string[] pids=File.ReadAllLines(ProcessPidLog);
            Assert.AreEqual(3, pids.Length, "Must create 3 processes");
            Process proc1 = Process.GetProcessById(Int32.Parse(pids[0]));
            Process proc2 = Process.GetProcessById(Int32.Parse(pids[1]));
            Assert.NotNull(proc1);
            Assert.NotNull(proc2);
            //dont check third process because it should be exited

            //kill and check processes are not live
            launcher.DotnetKill(new string[] { "kill", "--out", OutFolder });
            System.Threading.Thread.Sleep(500);
            proc1.Refresh();
            Assert.IsTrue(proc1.HasExited);
            proc2.Refresh();
            Assert.IsTrue(proc2.HasExited);

            //check final state of logs
            Assert.IsFalse(File.Exists(ProcessPidLog), "pid file should be deleted");
            string[] out1 = File.ReadAllLines(Process1OutLog);
            Assert.AreEqual("Start TestProcess:", out1[0], "Must write startup info");
            Assert.AreEqual("Timer 1", out1[1], "Must write timer info");
            string[] out2 = File.ReadAllLines(Process2OutLog);
            Assert.AreEqual("Start TestProcess:ab cd", out2[0], "Must write startup info");
            Assert.AreEqual("Timer 1", out2[1], "Must write timer info");
            string[] outx = File.ReadAllLines(ProcessxOutLog);
            Assert.AreEqual("Start TestProcess:exit", outx[0], "Must write startup info");
            Assert.AreEqual("end", outx[1], "Must exit");
        }

    }
}