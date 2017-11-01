using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sick.EasyRanger;
using Sick.EasyRanger.Controls;

namespace ControlClassLibrary
{
    class ENVResolveClass
    {
        private ProcessingEnvironment env;

        XmlResolveClass xmlRes;


        public ENVResolveClass(ProcessingEnvironment ezrEnz)
        {
            env = ezrEnz;
            xmlRes = new XmlResolveClass();
        }

        

        //Get Program
        public List<string> ProgramNameList()
        {

            List<string> programNameList = new List<string>();
            foreach (StepProgram stepProgram in env.Programs)
            {
                programNameList.Add(stepProgram.Name);
            }
            return programNameList;
        }

        //Get Step Name
        public List<string> StepNameList(StepProgram stepProgram)
        {
            List<string> stepNameList = new List<string>();
            foreach(Step stp in stepProgram.StepList)
            {
                stepNameList.Add(stp.Name);
            }
            return stepNameList;
        }

        //Get Program Var
        public List<string> VarNameList()
        {
            List<string> varNameList = new List<string>();
            foreach (Variable var in env.Variables)
            {
                varNameList.Add(var.Name);
            }
            return varNameList;
        }

        public Dictionary<string, string> getValueList()
        {
            Dictionary<string, string> varDict = new Dictionary<string, string>();
            List<string> varList = new List<string>();
            varList = xmlRes.getEnableVariables(XmlResolveClass.VariableType.Number);
            foreach(string str in varList)
            {
                if(env.GetDouble(str).Length > 0)
                {
                    double temp = env.GetDouble(str)[0];
                    varDict.Add(str, temp.ToString("F4"));

                }
                else
                {
                    //Inspection failed return NaN
                    varDict.Add(str, "NaN");
                }

            }
            return varDict;
        }




        
    }
}
