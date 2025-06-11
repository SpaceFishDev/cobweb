using System;
using System.Reflection.Emit;
using System.Diagnostics;
namespace Cobweb{
    public class Compiler
    {
        List<Instruction> Instructions = new();
        public int Position;
        public Instruction Current
        {
            get
            {
                if (Position > Instructions.Count)
                {
                    return Instructions[Instructions.Count - 1];
                }
                return Instructions[Position];
            }
        }
        public List<string> _Functions = new();
        public Compiler(List<Instruction> ins, List<Function> functions)
        {
            foreach (var func in functions)
            {
                _Functions.Add(func.Name);
            }
            Instructions = ins;
            FindAllStrings();
            BuildStringTable();
            FindAllDoubles();
            BuildDoubleTable();
            string stdLib = File.ReadAllText("std/std.asm");
            var data = stdLib.Split("section")[1];
            data = data.Replace(".data", "");
            var text = stdLib.Split("section")[2];
            text = text.Replace(".text", "");
            data_section += data + "\n";
            text_section += text + "\n";
        }
        string data_section = "section .data\n";
        string text_section = "section .text\nglobal main\n";
        List<string> ProgramStrings = new();
        Dictionary<int, float> ProgramDoubles = new();
        public void FindAllStrings()
        {
            foreach (Instruction ins in Instructions)
            {
                if (ins.Type == InstructionType.PUSH)
                {
                    if (ins.Arguments[0].Type == ArgType.STRING)
                    {
                        ProgramStrings.Add(ins.Arguments[0].Value.Remove(0, 1));
                    }
                }
            }
        }
        public void FindAllDoubles()
        {
            int p = 0;
            foreach (Instruction ins in Instructions)
            {
                if (ins.Type == InstructionType.PUSH)
                {
                    if (ins.Arguments[0].Type == ArgType.NUMBER)
                    {
                        ProgramDoubles.Add(p, float.Parse(ins.Arguments[0].Value));
                    }
               }
                ++p;
            }
        }
        public void BuildStringTable()
        {
            int stringNum = 0;
            foreach (string str in ProgramStrings)
            {
                data_section += $"string_{stringNum}: db \"{str}\", 0\n";
                stringNum++;
            }
        }
        public void BuildDoubleTable()
        {
            var keys = ProgramDoubles.Keys.ToList();
            foreach (var key in keys)
            {
                data_section += $"float_{key}: dq {ProgramDoubles[key]}\n";
            }
        }
        public string CurrentFunction = "";
        public List<(string, Dictionary<string, int>)> Functions = new(); // Functions with arguments
        public string Compile()
        {
            string result = "";
            switch (Current.Type)
            {
                case InstructionType.LABEL:
                    {
                        if ((Position + 1) < Instructions.Count && Instructions[Position + 1].Type == InstructionType.ARG_DECL)
                        {
                            int stackPos = 16;
                            string label = $"{Current.Arguments[0].Value}:";
                            ++Position;
                            Dictionary<string, int> Arguments = new();
                            CurrentFunction = label;
                            while (Current.Type == InstructionType.ARG_DECL)
                            {
                                string type = Current.Arguments[0].Value;
                                string name = Current.Arguments[1].Value;
                                Arguments.Add(name, stackPos);
                                switch (type)
                                {
                                    case "Number":
                                        {
                                            stackPos += 8;
                                        }
                                        break;
                                    case "String":
                                        {
                                            stackPos += 8;
                                        }
                                        break;
                                }
                                ++Position;
                            }
                            Functions.Add((label.Replace(":", ""), Arguments));
                            result = label+ "\npush rbp\nmov rbp, rsp";
                        }
                        else
                        {
                            foreach (var f in _Functions)
                            {
                                if (f == Current.Arguments[0].Value)
                                {
                                    CurrentFunction = f;
                                    break;
                                }
                            }
                            if (CurrentFunction == Current.Arguments[0].Value)
                            {
                                result = $"{CurrentFunction}:\npush rbp\nmov rbp, rsp";
                                ++Position;
                            }
                            else
                            {
                                result = $"{Current.Arguments[0].Value}:";
                                ++Position;
                            }
                        }
                    }
                    break;
                case InstructionType.SUB:
                    {
                        result = "movsd xmm0, qword [rsp]\nadd rsp, 8\nmovsd xmm1, qword [rsp]\nsubsd xmm1, xmm0\nsub rsp, 8\nmovsd [rsp], xmm0";
                        ++Position;
                    } break;
                case InstructionType.ADD:
                    {
                        result = "movsd xmm0, qword [rsp]\nadd rsp, 8\nmovsd xmm1, qword [rsp]\naddsd xmm1, xmm0\nsub rsp, 8\nmovsd [rsp], xmm0";
                        ++Position;
                    } break;
                case InstructionType.MUL:
                    {
                        result = "movsd xmm0, qword [rsp]\nadd rsp, 8\nmovsd xmm1, qword [rsp]\nmulsd xmm1, xmm0\nsub rsp, 8\nmovsd [rsp], xmm0";
                        ++Position;
                        
                    } break;
                case InstructionType.DIV:
                    {
                        result = "movsd xmm0, qword [rsp]\nadd rsp, 8\nmovsd xmm1, qword [rsp]\ndivsd xmm1, xmm0\nsub rsp, 8\nmovsd [rsp], xmm0";
                        ++Position;
                    } break;
                case InstructionType.PUSH:
                    {
                        if (Current.Arguments[0].Type == ArgType.STRING)
                        {
                            string str = Current.Arguments[0].Value.Remove(0, 1);
                            int n = 0;
                            foreach (var dat in ProgramStrings)
                            {
                                if (dat == str)
                                {
                                    result = $"push string_{n}";
                                    break;
                                }
                                ++n;
                            }
                            ++Position;
                        }
                        else if (Current.Arguments[0].Type == ArgType.NUMBER)
                        {
                            result = $"movsd xmm0, qword [float_{Position}]\nsub rsp, 8\nmovsd [rsp], xmm0";
                            ++Position;
                        }
                        else if(Current.Arguments[0].Type == ArgType.VARIABLE)
                        {
                            (string name, Dictionary<string, int> args) func = ("", new());
                            foreach (var f in Functions)
                            {
                                if (f.Item1 == CurrentFunction.Replace(":", ""))
                                {
                                    func = f;
                                }
                            }
                            string name = Current.Arguments[0].Value;
                            if (!func.args.ContainsKey(name))
                            {
                                ++Position;
                                return "";
                            }

                            int stackPos = func.args[name];
                            result = $"movsd xmm0, qword [rbp + {stackPos}]\nsub rsp, 8\nmovsd [rsp], xmm0";

                            ++Position;
                        }
                    }
                    break;
                case InstructionType.CONDITIONAL_JUMP:
                    {
                        Instruction prev = Instructions[Position - 1];
                        result = "movsd xmm0, qword [rsp]\nadd rsp, 8\nmovsd xmm1, qword [rsp]\nadd rsp, 8\nucomisd xmm1, xmm0\n";
                        switch (prev.Type)
                        {
                            case InstructionType.CMP_EQ:
                                {
                                    result += $"je {Current.Arguments[0].Value}";
                                } break;
                            case InstructionType.CMP_LESS:
                                {
                                    result += $"jb {Current.Arguments[0].Value}";
                                } break;
                            case InstructionType.CMP_MORE:
                                {
                                    result += $"ja {Current.Arguments[0].Value}";
                                }   break;
                        }
                        ++Position;
                    }break;
                case InstructionType.CALL:
                    {
                        result = $"call {Current.Arguments[0].Value}";
                        ++Position;
                    } break;
                case InstructionType.RETURN:
                    {
                        if (CurrentFunction == "main")
                        {
                            result = $"movsd xmm0, qword [rsp]\ncvtsd2si rax, xmm0\nmov rdi, rax\nmov rax, 60\nsyscall";
                            ++Position;
                            return result;
                        }
                        result = $"mov rsp, rbp\npop rbp\nret";
                        ++Position;
                    } break;
                case InstructionType.JMP:
                    {
                        result += $"jmp {Current.Arguments[0].Value}";
                        ++Position;
                    } break;
                default:
                    {
                        ++Position;
                    }
                    break;
            }
            return result;
        }
        public string CompileAll()
        {
            string res = "";
            while (Position < Instructions.Count)
            {
                string comp = Compile();
                if (comp != "")
                {
                    res += comp + "\n";
                }
            }
            text_section += res;
            return data_section + "\n" + text_section;

        }
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
            // foreach (var F in preprocesser.Functions)
            // {
                // Console.WriteLine(F);
            // }
            // Console.WriteLine("IL:");
            // Console.WriteLine(generator.OutputSrc);
            BytecodeGenerator bytecodeGenerator = new(generator.OutputSrc);
            bytecodeGenerator.ParseAllInstructions();
            // foreach (Instruction ins in bytecodeGenerator.Instructions)
            // {
                // Console.WriteLine(ins);
            // }


            Compiler compiler = new(bytecodeGenerator.Instructions, generator.Functions);
            string asm = compiler.CompileAll();
            File.WriteAllText(OutputFile + ".asm", asm);
            Process.Start("nasm", $"\"{OutputFile + ".asm"}\" -f elf64 -o \"{OutputFile}.o\"");
            Process.Start("gcc", $"\"{OutputFile}.o\" -no-pie -o \"{OutputFile}\"");
        }
    }
}