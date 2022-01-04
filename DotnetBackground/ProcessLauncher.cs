using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DotnetBackground
{
    /// <summary>
    /// Manages the main methods to run and kill processes
    /// </summary>
    public class ProcessLauncher
    {
        public const string PidFileName = "dotnet-background-pids.log"; //stores the PIDs of launched processes

        public void DotnetRun(string[] args)
        {
            //Parse arguments and sets output/error redirection files
            CustomArguments CustomArgs = new CustomArguments();
            args = CustomArgs.Parse(args);
            string outFile = Path.Combine(CustomArgs.OutDir, "dotnet-" + CustomArgs.Name + "-out.log");
            string errFile = Path.Combine(CustomArgs.OutDir, "dotnet-" + CustomArgs.Name + "-err.log");
            string command = string.Join(" ", args);
            if (CustomArgs.OutDir != ".")
                Directory.CreateDirectory(CustomArgs.OutDir);

            //Launch the dotnet run command inside a shell and stores the main process pid
            Int32 pid = RunCommand(command, outFile, errFile);
            Console.WriteLine("dotnet-background: run pid " + pid + " [" + CustomArgs.Name + "] command: " + command);
            File.AppendAllText(Path.Combine(CustomArgs.OutDir, PidFileName), pid.ToString() + "\n");
        }
        public void DotnetKill(string[] args)
        {
            //Read the pids of previously launched processes
            CustomArguments CustomArgs = new CustomArguments();
            CustomArgs.Parse(args);
            string pidFile = Path.Combine(CustomArgs.OutDir, PidFileName);
            string[] pids;
            //Lee el fichero con los pid, finaliza silenciosamente si no existe
            try
            {
                pids = File.ReadAllLines(pidFile);
            }
            catch (Exception)
            {
                throw new CustomArgumentException("Can't read the process pid file: " + PidFileName);
            }

            //Kills each process
            foreach (string pid in pids)
            {
                Console.WriteLine("dotnet-background: kill pid " + pid + " and children");
                try
                {
                    Stop(Int32.Parse(pid));
                }
                catch (Exception) { }
            }
            File.Delete(pidFile);
        }

        private Int32 RunCommand(string cmd, string outFile, string errFile)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Start("cmd", "/c (dotnet " + cmd + ")" + " >" + outFile + " 2>" + errFile);
            else //bash reqiere encerrar entre comillas el comando
                return Start("bash", "-c \"(dotnet " + cmd + ")" + " >" + outFile + " 2>" + errFile + "\"");
        }

        private Int32 Start(string name, string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = name,
                Arguments = args,
                //RedirectStandardOutput = false, //redirection is made by the shell
                UseShellExecute = true,
                //CreateNoWindow = true, //requiers useShellExecute false, if not, ignored
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            Process proc = new Process { StartInfo = startInfo };
            proc.Start();
            return proc.Id;
        }
        public void Stop(Int32 pid)
        {
            Process proc = Process.GetProcessById(pid);
            proc.Kill(true);
        }
    }

}
