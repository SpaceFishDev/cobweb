using System;

namespace Cobweb{

    public class Interpreter
    {
        
    }
    public class Program
    {
        public static string InputFile = "main.cbw";
        public static string OutputFile = "a.o";

        public static void Main(string[] Args)
        {
            int i = 0;
            foreach (var arg in Args)
            {
                if (arg == "-o")
                {
                    if (Args.Length < i + 1)
                    {
                        Console.WriteLine("After '-o' an output file is required.");
                        return;
                    }
                    OutputFile = Args[i + 1];
                }
                else
                {
                    if (arg != OutputFile)
                    {
                        InputFile = arg;
                    }
                }
                ++i;
            }
            if (!File.Exists(InputFile))
            {
                Console.WriteLine($"File {InputFile} Doesnt exist.");
                return;
            }
            string src = File.ReadAllText(InputFile);
            Parser parser = new(src);
            Preprocesser preprocesser = new(parser.Tree);
            preprocesser.FindVariables();
            preprocesser.FindTypes();
            preprocesser.FindFunctionTypes();
            IlGenerator generator = new(preprocesser.Tree, preprocesser.Functions);
            generator.GenerateIl();
            Console.WriteLine(parser.Tree);
            foreach (var F in preprocesser.Functions)
            {
                Console.WriteLine(F);
            }
            Console.WriteLine("IL:");
            Console.WriteLine(generator.OutputSrc);
            BytecodeGenerator bytecodeGenerator = new(generator.OutputSrc);
            bytecodeGenerator.ParseAllInstructions();
        }
    }
}