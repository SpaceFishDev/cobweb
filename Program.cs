using System;
using System.Reflection.Emit;
using System.Diagnostics;
namespace Cobweb{
    public class Interpreter
    {
        public List<Instruction> Instructions;
        public List<Function> Functions;
        public Interpreter(List<Instruction> ins, List<Function> funs)
        {
            Instructions = ins;
            Functions = funs;
            Memory = new byte[1024 * 1024];
            Stack = new byte[1024 * 1024];
            ArgsD = new();
            ArgsI = new();
            ProgramStrings = new();
            CallStack = new();
            int mempos = 0;
            foreach (var i in ins)
            {
                if (i.Type == InstructionType.PUSH && i.Arguments[0].Type == ArgType.STRING)
                {
                    int pos = mempos;
                    foreach (char c in i.Arguments[0].Value)
                    {
                        byte b = (byte)c;
                        Memory[mempos] = b;
                        ++mempos;
                    }
                    mempos++;
                    ProgramStrings.Add((i.Arguments[0].Value, pos));
                } 
            }
            CurrentFunction = "";
        }
        public string CurrentFunction;
        public List<double> ArgsD;
        public List<int> ArgsI; // for pointers 
        public List<(List<int> ArgsI, List<double> ArgsD, int Pos, string Name)> CallStack;
        public byte[] Memory;
        public byte[] Stack;
        public int Pos;
        public double Dreturn;
        public int Ireturn;
        public List<(string, int)> ProgramStrings;
        public Instruction Current
        {
            get
            {
                return Instructions[Pos];
            }
        }
        int stackPointer = 1024 * 1024;
        public void PushBytes(byte[] bytes)
        {
            stackPointer -= bytes.Length;
            int i = 0;
            foreach (byte b in bytes)
            {
                Stack[stackPointer + i] = b;
                ++i;
            }   
        }
        public void Push()
        {
            switch (Current.Arguments[0].Type)
            {
                case ArgType.NUMBER:
                {
                        double val = double.Parse(Current.Arguments[0].Value);
                        var bytes = BitConverter.GetBytes(val);
                        PushBytes(bytes);
                } break;
                case ArgType.STRING:
                    {
                        stackPointer -= sizeof(int);
                        int val = 0;
                        foreach (var s in ProgramStrings)
                        {
                            if (s.Item1 == Current.Arguments[0].Value)
                            {
                                val = s.Item2;
                                break;
                            }
                        }
                        var bytes = BitConverter.GetBytes(val);
                        PushBytes(bytes);
                    } break;
                case ArgType.VARIABLE:
                    {
                        int Didx = 0;
                        int Iidx = 0;
                        Function func = new();
                        foreach (var f in Functions)
                        {
                            if (f.Name == CurrentFunction)
                            {
                                func = f;
                            }
                        }
                        Variable variable = new();
                        foreach (var v in func.Args)
                        {
                            if (v.Name == Current.Arguments[0].Value)
                            {
                                variable = v;
                                break;
                            }
                            if (v.Type == VariableType.Number)
                            {
                                ++Didx;
                            }
                            if (v.Type == VariableType.Str)
                            {
                                ++Iidx;   
                            }
                            if (v.Type == VariableType.List)
                            {
                                Iidx += 2;
                            }
                        }
                        if (variable.Type == VariableType.Number)
                        {
                            double value = ArgsD[Didx];
                            var bytes = BitConverter.GetBytes(value);
                            PushBytes(bytes);
                        }
                        if (variable.Type == VariableType.Str)
                        {
                            int value = ArgsI[Iidx];
                            var bytes = BitConverter.GetBytes(value);
                            PushBytes(bytes);
                        }
                    } break;
            } 
        }
        public double PopNum()
        {
            double value = 0;
            byte[] bytes = new byte[sizeof(double)];
            stackPointer += sizeof(double);
            int i = stackPointer - sizeof(double);
            int n = 0;
            for (; i < stackPointer; ++i)
            {
                bytes[n] = Stack[i];
                ++n;
            }
            value = BitConverter.ToDouble(bytes);
            return value;   
        }
        public int PopI()
        {
            int value = 0;
            stackPointer += sizeof(int);
            int i = stackPointer - sizeof(int);
            int n = 0;
            byte[] bytes = new byte[sizeof(int)];
            for (; i < stackPointer; ++i)
            {
                bytes[n] = Stack[i];
                ++n;
            }
            value = BitConverter.ToInt32(bytes);
            return value;
        }
        public void CallFunction()
        {
            int pos = Pos;
            double[] arrD = new double[ArgsD.Count];
            int[] arrI = new int[ArgsI.Count];
            ArgsD.CopyTo(arrD);
            ArgsI.CopyTo(arrI);
            CallStack.Add((arrI.ToList(), arrD.ToList(), pos+1, CurrentFunction));
            Function func = new();
            foreach (Function f in Functions)
            {
                if (f.Name == Current.Arguments[0].Value)
                {
                    func = f;
                }
            }
            pos = 0;
            foreach (var ins in Instructions)
            {
                if (ins.Type == InstructionType.LABEL)
                {
                    if (ins.Arguments[0].Value == func.Name)
                    {
                        break;
                    }
                }
                ++pos;
            }
            Pos = pos;
            int idxD = 0;
            int idxI = 0;
            if (func.Args == null)
            {
                func.Args = new();
            }
            foreach (var v in func.Args)
                {
                    if (v.Type == VariableType.Number)
                    {
                        double d = PopNum();
                        ArgsD.Add(d);
                        ++idxD;
                    }
                    if (v.Type == VariableType.Str)
                    {
                        int i = PopI();
                        ArgsI.Add(i);
                        ++idxI;
                    }
                    if (v.Type == VariableType.List)
                    {
                        int i = PopI();
                        ArgsI[idxI] = i;
                        ++idxI;
                        i = PopI();
                        ArgsI[idxI] = i;
                        ++idxI;
                    }
                }
        }
        public void ReturnFromFunction()
        {
            Function f = new();
            foreach (var func in Functions)
            {
                if (func.Name == CurrentFunction)
                {
                    f = func;
                }
            }
            if (CallStack.Count == 0)
            {
                Pos = Instructions.Count + 1;
                return;
            }
            CallStack.RemoveAt(CallStack.Count - 1);
            var call = CallStack[CallStack.Count - 1];
            CurrentFunction = call.Name;
            ArgsD = call.ArgsD;
            ArgsI = call.ArgsI;
            Pos = call.Pos;
            if (f.Type == VariableType.Number)
            {
                double d = PopNum();
                Dreturn = d;
            }
            if (f.Type == VariableType.Str)
            {
                int i = PopI();
                Ireturn = i;
            }
        }
        public void Run()
        {
            for (int i = 0; i < Instructions.Count; ++i)
            {
                if (Current.Type == InstructionType.LABEL)
                {
                    if (Current.Arguments[0].Value == "main")
                    {
                        break;
                    }
                }
                ++Pos;
            }
            CurrentFunction = "main";
            while (Pos < Instructions.Count)
            {
                switch (Current.Type)
                {
                    case InstructionType.PUSH:
                    {
                            Push();
                    } break;
                    case InstructionType.ADD:
                        {
                            double a = PopNum();
                            double b = PopNum();
                            double res = a + b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.SUB:
                        {
                            double a = PopNum();
                            double b = PopNum();
                            double res = a - b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.MUL:
                        {
                            double a = PopNum();
                            double b = PopNum();
                            double res = a * b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.DIV:
                        {
                            double a = PopNum();
                            double b = PopNum();
                            double res = a / b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.CALL:
                        {
                            CallFunction();
                        } break;
                    case InstructionType.RETURN:
                        {
                            ReturnFromFunction();
                        } break;
                }
                ++Pos;
            }
        }
        public void StackDump()
        {
            Console.WriteLine("Stack:");
            for (int i = stackPointer; i < 1024 * 1024; ++i)
            {
                Console.WriteLine($"{(1024*1024)-i}: {Stack[i]}");
            }
        }
    }
    public class Program
    {
        public static string InputFile = "main.cbw";
        public static string OutputFile = "a.o";

        public static void Main(string[] Args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            bool interpret = true;
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
                    interpret = false;
                }
                else if (arg == "-i")
                {
                    interpret = true;
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
            // foreach (var F in preprocesser.Functions)
            // {
            // Console.WriteLine(F);
            // }
            Console.WriteLine("IL:");
            Console.WriteLine(generator.OutputSrc);
            BytecodeGenerator bytecodeGenerator = new(generator.OutputSrc);
            bytecodeGenerator.ParseAllInstructions();
            foreach (Instruction ins in bytecodeGenerator.Instructions)
            {
                Console.WriteLine(ins);
            }
            if (!interpret)
            {
                Compiler compiler = new(bytecodeGenerator.Instructions, generator.Functions);
                File.WriteAllText(OutputFile + ".asm", compiler.CompiledSource);
                // Console.WriteLine(compiler.CompiledSource);
            }
            if (interpret)
            {
                Interpreter interpreter = new(bytecodeGenerator.Instructions, generator.Functions);
                interpreter.Run();
                interpreter.StackDump();
                double d = interpreter.PopNum();
                Console.WriteLine($"Return Value: {d}");
            }
        }
    }
}