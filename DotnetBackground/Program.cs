using System;
using System.Reflection;

namespace DotnetBackground
{
    /**
     * Ejecucion y parada de un proyecto .net desde consola. Remplaza el script powershel usado en 2019:
     * Motivacion: Se podria ejecutar desde cmd con start dotnet run ... y parar con taskkill /im dotnet.exe /f
     * pero esto detendra todos los procesos dotnet que se esten ejecutando en el servidor de CI (incluyendo compilaciones).
     * El comando dotnet run no permite obtener el id del proceso, y aunque se consiga el id del proceso ejecutado,
     * tambien lanza varios subprocesos que deben ser finalizado.

     * Este program permite ejecutar diferentes comandos:
     * run: Ejecuta un proceso. Requiere un parametro con el nombre del proyecto y otros opcionales con los argumentos a pasar.
     *      Redirige la consola (salida y error) a ficheros situados en reportDir.
     *      Anyade a un archivo dotnet-pids.log en reportDir el pid del proceso creado para que se pueda borrar posteriormente con kill
     * kill: Detiene todos los procesos que han sido ejecutados con run. Solo disponible en net core 3.1 pues
     *       utiliza el argumento que indica detener todos los procesos hijo
     *
     * Funcionamiento: 
     * - Las invocaciones a run crean procesos y guardan los pid en un fichero externo en reports
     * - La posterior invocacion a kill lee este fichero y detiene los procesos con los pid indicads en este
     *
     * Se supone que se ejecuta en la carpeta de la solucion,
     * de los paremetros, para kill se necesita solo el 2 
     * command   Args[0] #run o kill
     * srcDir    Args[1] #requerido (localizacion de los proyectos)
     * reportDir Args[2] #requerido (localizacion de reports)
     * project   Args[3] #requerido para comando run (nombre del proyecto, debe coincidir con la carpeta y el .csproj)
     * args      Args[4] #opcional, argumentos de net run (no deben contener comillas)
     */
    ///

    class Program
    {
        /// <summary>
        /// An easy way to launch dotnet run background processes and kill each process and their descendants with a single command.
        /// See usage instructions at: https://github.com/javiertuya/dotnet-background#readme
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                MainLaunch(args);
            }
            catch (CustomArgumentException e)
            {
                var versionString = Assembly.GetEntryAssembly()?
                       .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                       .InformationalVersion
                       .ToString();
                Console.WriteLine($"DotnetBackground v{versionString}");
                Console.Error.WriteLine("ERROR: " + e.Message);
                Console.Error.WriteLine("See usage instructions at: https://github.com/javiertuya/dotnet-background#readme");
            }
        }
        static void MainLaunch(string[] args)
        {
            ProcessLauncher launcher = new ProcessLauncher();
            if (args.Length==0)
                throw new CustomArgumentException("Required command: run or kill");
            string command = args[0];
            if (command == "run")
                launcher.DotnetRun(args);
            else if (command == "kill")
                launcher.DotnetKill(args);
            else
                throw new CustomArgumentException("Command not allowed: " + command);
        }
    }

}
