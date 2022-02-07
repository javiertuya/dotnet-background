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


        private const string OutFolder = "../../../../reports/nunit";
        private const string ProcessPidLog = "../../../../reports/nunit/" + ProcessLauncher.PidFileName;
        private const string Process1OutLog = "../../../../reports/nunit/dotnet-TestProcess-out.log";
        private const string Process1ErrLog = "../../../../reports/nunit/dotnet-TestProcess-err.log";
        private const string Process2OutLog = "../../../../reports/nunit/dotnet-CustomNamed-out.log";
        private const string Process2ErrLog = "../../../../reports/nunit/dotnet-CustomNamed-err.log";
        private const string ProcessxOutLog = "../../../../reports/nunit/dotnet-ExitedProcess-out.log";
        private const string ProcessxErrLog = "../../../../reports/nunit/dotnet-ExitedProcess-err.log";

        private string GetTargetFrameworkMoniker()
        {
            string framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            if (framework.StartsWith(".NET Core 3.1."))
                return "netcoreapp3.1";
            else if (framework.StartsWith(".NET 5.0."))
                return "net5.0";
            else
                throw new Exception("Runtime framework not allowed for this test: " + framework);
        }
        [Test]
        public void TestWorkflow()
        {
            string tfm = GetTargetFrameworkMoniker();
            System.Console.Out.WriteLine("Running on: "+tfm);
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

            //Launchand wait (using the TFM that correspond with the framework executing this test) 
            String outParam = " --out ../../../../reports/nunit";
            String projectParam = " --project ../../../../TestProcess/TestProcess.csproj --no-restore --framework " + tfm;
            ProcessLauncher launcher = new ProcessLauncher();
            launcher.DotnetRun(("run" + outParam + projectParam).Split(" "));
            launcher.DotnetRun(("run --name CustomNamed" + outParam + projectParam + " ab cd").Split(" "));
            launcher.DotnetRun(("run --name ExitedProcess" + outParam + projectParam + " exit").Split(" "));
            System.Threading.Thread.Sleep(5000); //time to write some additional line

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