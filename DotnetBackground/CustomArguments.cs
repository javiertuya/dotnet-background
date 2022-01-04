using System;
using System.Collections.Generic;
using System.IO;

namespace DotnetBackground
{
    /// <summary>
    /// Stores and manages the values of arguments that are not pased to dotnet run, but needed to launch the process
    /// </summary>
    public class CustomArguments
    {
        public string OutDir { get; set; } = ".";
        public string Name { get; set; } = "";

        /// <summary>
        /// Parses the arguments, returning all arguments except those that are custom, which are stored in this classs
        /// </summary>
        public string[] Parse(string[] args)
        {
            List<string> argList = new List<string>();
            argList.AddRange(args);
            //from end to begin to remove detected custom arguments
            for (int i = argList.Count - 1; i >= 0; i--)
            {
                if (argList[i] == "--out")
                    this.OutDir = ParseParam(argList, i, true);
                else if (argList[i] == "--name")
                    this.Name = ParseParam(argList, i, true);
                else if (argList[i] == "--project" && string.IsNullOrEmpty(this.Name)) 
                {
                    //not remove, but extract name from project path if not set
                    this.Name = ParseParam(argList, i, false);
                    this.Name = Path.GetFileNameWithoutExtension(this.Name);
                }
            }
            return argList.ToArray();
        }
        private string ParseParam(List<string> argList, int position, bool removeArg)
        {
            if (position + 1 >= argList.Count)
                throw new CustomArgumentException("Parameter " + argList[position] + " requires a value");
            if (argList[position + 1].StartsWith("--"))
                throw new CustomArgumentException("Parameter " + argList[position] + " requires a value but starts with --");
            string value = argList[position + 1];
            if (removeArg)
            {
                argList.RemoveAt(position + 1);
                argList.RemoveAt(position);
            }
            return value;
        }
    }

    public class CustomArgumentException : Exception
    {
        public CustomArgumentException(string message) : base(message) { }
    }
}
