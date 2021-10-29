using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleComponents;

namespace Machine
{
    public class CPU16 
    {
        //this "enum" defines the different control bits names
        public const int J3 = 0, J2 = 1, J1 = 2, D3 = 3, D2 = 4, D1 = 5, C6 = 6, C5 = 7, C4 = 8, C3 = 9, C2 = 10, C1 = 11, A = 12, X2 = 13, X1 = 14, Type = 15;

        public int Size { get; private set; }

        //CPU inputs
        public WireSet Instruction { get; private set; }
        public WireSet MemoryInput { get; private set; }
        public Wire Reset { get; private set; }

        //CPU outputs
        public WireSet MemoryOutput { get; private set; }
        public Wire MemoryWrite { get; private set; }
        public WireSet MemoryAddress { get; private set; }
        public WireSet InstructionAddress { get; private set; }

        //CPU components
        private ALU m_gALU;
        private Counter m_rPC;
        private MultiBitRegister m_rA, m_rD;
        private BitwiseMux m_gAMux, m_gMAMux;

        //here we initialize and connect all the components, as in Figure 5.9 in the book
        public CPU16()
        {
            Size =  16;

            Instruction = new WireSet(Size);
            MemoryInput = new WireSet(Size);
            MemoryOutput = new WireSet(Size);
            MemoryAddress = new WireSet(Size);
            InstructionAddress = new WireSet(Size);
            MemoryWrite = new Wire();
            Reset = new Wire();

            m_gALU = new ALU(Size);
            m_rPC = new Counter(Size);
            m_rA = new MultiBitRegister(Size);
            m_rD = new MultiBitRegister(Size);

            m_gAMux = new BitwiseMux(Size);
            m_gMAMux = new BitwiseMux(Size);

            m_gAMux.ConnectInput1(Instruction);
            m_gAMux.ConnectInput2(m_gALU.Output);

            m_rA.ConnectInput(m_gAMux.Output);

            m_gMAMux.ConnectInput1(m_rA.Output);
            m_gMAMux.ConnectInput2(MemoryInput);
            m_gALU.InputY.ConnectInput(m_gMAMux.Output);

            m_gALU.InputX.ConnectInput(m_rD.Output);

            m_rD.ConnectInput(m_gALU.Output);

            MemoryOutput.ConnectInput(m_gALU.Output);
            MemoryAddress.ConnectInput(m_rA.Output);

            InstructionAddress.ConnectInput(m_rPC.Output);
            m_rPC.ConnectInput(m_rA.Output);
            m_rPC.ConnectReset(Reset);

            //now, we call the code that creates the control unit
            ConnectControls();
        }

        //add here components to implement the control unit 
        private BitwiseMultiwayMux m_gJumpMux;//an example of a control unit compnent - a mux that controls whether a jump is made
        private NotGate m_gNotMSB;
        private AndGate m_gAndA;
        private OrGate m_gOrA;
        private AndGate m_gAndWrite;
        private AndGate m_gAndD;

        private WireSet control;
        private WireSet [] input;

        private AndGate m_gAndIn0;
        private AndGate m_gAndIn1;
        private AndGate m_gAndIn2;
        private AndGate m_gAndIn3;
        private AndGate m_gAndIn4;
        private AndGate m_gAndIn6;
        private AndGate m_gAndIn7;

        private AndGate m_gAndIn5;

        private AndGate m_gAndIn11;
        private AndGate m_gAndIn22;
        private AndGate m_gAndIn33;
        private AndGate m_gAndIn44;
        private AndGate m_gAndIn66;


        private NotGate m_gNotZ;
        private NotGate m_gNotN;

        private Wire w0;
        private Wire w1;

        private void ConnectControls()
        {
            //1. connect control of mux 1 (selects entrance to register A)
            m_gAMux.ConnectControl(Instruction[Type]);

            //2. connect control to mux 2 (selects A or M entrance to the ALU)
            m_gMAMux.ConnectControl(Instruction[A]);

            //3. consider all instruction bits only if C type instruction (MSB of instruction is 1)

            //4. connect ALU control bits
            m_gALU.ZeroX.ConnectInput(Instruction[C1]);
            m_gALU.NotX.ConnectInput(Instruction[C2]);
            m_gALU.ZeroY.ConnectInput(Instruction[C3]);
            m_gALU.NotY.ConnectInput(Instruction[C4]);
            m_gALU.F.ConnectInput(Instruction[C5]);
            m_gALU.NotOutput.ConnectInput(Instruction[C6]);

            //5. connect control to register D (very simple)
            m_gAndD = new AndGate();
            m_gAndD.ConnectInput1(Instruction[Type]);
            m_gAndD.ConnectInput2(Instruction[D2]);
            m_rD.Load.ConnectInput(m_gAndD.Output);

            //6. connect control to register A (a bit more complicated)
            m_gNotMSB = new NotGate();
            m_gAndA = new AndGate();
            m_gOrA = new OrGate();
            m_gNotMSB.ConnectInput(Instruction[Type]);
            m_gAndA.ConnectInput1(Instruction[D1]);
            m_gAndA.ConnectInput2(Instruction[Type]);
            m_gOrA.ConnectInput1(m_gNotMSB.Output);
            m_gOrA.ConnectInput2(m_gAndA.Output);
            m_rA.Load.ConnectInput(m_gOrA.Output);

            //7. connect control to MemoryWrite
            m_gAndWrite = new AndGate();
            m_gAndWrite.ConnectInput1(Instruction[Type]);
            m_gAndWrite.ConnectInput2(Instruction[D3]);
            MemoryWrite.ConnectInput(m_gAndWrite.Output);

            //8. create inputs for jump mux
            input = new WireSet[8];
            m_gAndIn0 = new AndGate();
            m_gAndIn1 = new AndGate();
            m_gAndIn2 = new AndGate();
            m_gAndIn3 = new AndGate();
            m_gAndIn4 = new AndGate();
            m_gAndIn5 = new AndGate();
            m_gAndIn6 = new AndGate();
            m_gAndIn7 = new AndGate();

            m_gAndIn11 = new AndGate();
            m_gAndIn22 = new AndGate();
            m_gAndIn33 = new AndGate();
            m_gAndIn44 = new AndGate();
            m_gAndIn66 = new AndGate();
            m_gNotZ = new NotGate();
            m_gNotN = new NotGate();
            w0 = new Wire();
            w1 = new Wire();
           

            for (int i = 0; i < 8; i++)
                input[i] = new WireSet(1);

            m_gAndIn0.ConnectInput1(w0);
            m_gAndIn0.ConnectInput2(Instruction[Type]);
            input[0][0].ConnectInput(m_gAndIn0.Output);
            m_gAndIn7.ConnectInput1(w1);
            m_gAndIn7.ConnectInput2(Instruction[Type]);
            input[7][0].ConnectInput(m_gAndIn7.Output);

            m_gNotZ.ConnectInput(m_gALU.Zero);
            m_gNotN.ConnectInput(m_gALU.Negative);

            m_gAndIn1.ConnectInput1(m_gNotZ.Output);
            m_gAndIn1.ConnectInput2(m_gNotN.Output);
            m_gAndIn11.ConnectInput1(m_gAndIn1.Output);
            m_gAndIn11.ConnectInput2(Instruction[Type]);
            input[1][0].ConnectInput(m_gAndIn11.Output);

            m_gAndIn2.ConnectInput1(m_gALU.Zero);
            m_gAndIn2.ConnectInput2(m_gNotN.Output);
            m_gAndIn22.ConnectInput1(m_gAndIn2.Output);
            m_gAndIn22.ConnectInput2(Instruction[Type]);
            input[2][0].ConnectInput(m_gAndIn22.Output);

            m_gAndIn3.ConnectInput1(m_gALU.Zero);
            m_gAndIn3.ConnectInput2(m_gNotN.Output);
            m_gAndIn33.ConnectInput1(m_gAndIn3.Output);
            m_gAndIn33.ConnectInput2(Instruction[Type]);
            input[3][0].ConnectInput(m_gAndIn33.Output);

            m_gAndIn4.ConnectInput1(m_gNotZ.Output);
            m_gAndIn4.ConnectInput2(m_gALU.Negative);
            m_gAndIn44.ConnectInput1(m_gAndIn4.Output);
            m_gAndIn44.ConnectInput2(Instruction[Type]);
            input[4][0].ConnectInput(m_gAndIn44.Output);


            m_gAndIn5.ConnectInput1(m_gNotZ.Output);
            m_gAndIn5.ConnectInput2(Instruction[Type]);
            input[5][0].ConnectInput(m_gAndIn5.Output);

            m_gAndIn6.ConnectInput1(m_gALU.Zero);
            m_gAndIn6.ConnectInput2(m_gALU.Negative);
            m_gAndIn66.ConnectInput1(m_gAndIn6.Output);
            m_gAndIn66.ConnectInput2(Instruction[Type]);
            input[6][0].ConnectInput(m_gAndIn66.Output);

            //9. connect jump mux (this is the most complicated part)
            m_gJumpMux = new BitwiseMultiwayMux(1, 3);
            control = new WireSet(3);
            control[0].ConnectInput(Instruction[J3]);
            control[1].ConnectInput(Instruction[J2]);
            control[2].ConnectInput(Instruction[J1]);
            m_gJumpMux.ConnectControl(control);
            for (int i=0;i<8;i++)
            {
                m_gJumpMux.ConnectInput(i, input[i]);
            }
            w0.Value = 0;
            w1.Value = 1;

            //10. connect PC load control
            m_rPC.ConnectLoad(m_gJumpMux.Output[0]);

        }

        public override string ToString()
        {
            return "A=" + m_rA + ", D=" + m_rD + ", PC=" + m_rPC + ",Ins=" + Instruction;
        }

        private string GetInstructionString()
        {
            if (Instruction[Type].Value == 0)
                return "@" + Instruction.GetValue();
            return Instruction[Type].Value + "XX " +
               "a" + Instruction[A] + " " +
               "c" + Instruction[C1] + Instruction[C2] + Instruction[C3] + Instruction[C4] + Instruction[C5] + Instruction[C6] + " " +
               "d" + Instruction[D1] + Instruction[D2] + Instruction[D3] + " " +
               "j" + Instruction[J1] + Instruction[J2] + Instruction[J3];
        }

        //use this function in debugging to print the current status of the ALU. Feel free to add more things for printing.
        public void PrintState()
        {
            Console.WriteLine("CPU state:");
            Console.WriteLine("PC=" + m_rPC + "=" + m_rPC.Output.GetValue());
            Console.WriteLine("A=" + m_rA + "=" + m_rA.Output.GetValue());
            Console.WriteLine("D=" + m_rD + "=" + m_rD.Output.GetValue());
            Console.WriteLine("Ins=" + GetInstructionString());
            Console.WriteLine("ALU=" + m_gALU);
            Console.WriteLine("inM=" + MemoryInput);
            Console.WriteLine("outM=" + MemoryOutput);
            Console.WriteLine("addM=" + MemoryAddress);
        }
    }
}
