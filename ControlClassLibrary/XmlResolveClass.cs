using ControlClassLibrary.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ControlClassLibrary
{
    class XmlResolveClass
    {

       
        string filePath = Resources.xmlPath + @"\EnvironmentXML.xml";
        XmlDocument xmlDoc;
        
        public enum VariableType
        {
            Number,
            Drawing
        };

        public List<string> getEnableVariables(VariableType type)
        {
            List<string> outputList = new List<string>();
            xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNode mainNode = xmlDoc.SelectSingleNode("EasyRangerEnv");
            XmlNode varNode = mainNode.FirstChild;
            XmlNodeList typeNodeList = varNode.ChildNodes;
            //输出特性 分类
            ///输出数值
            if(type == VariableType.Number)
            {
                foreach (XmlNode typeNode in typeNodeList)
                {
                    if (typeNode.Name == "Double" && typeNode.HasChildNodes)
                    {
                        foreach (XmlNode doubleValue in typeNode.ChildNodes)
                        {
                            if(doubleValue.Attributes.GetNamedItem("enable").Value == "1")
                            {
                                outputList.Add(doubleValue.InnerText);
                            }
                        }
                    }
                }

            }
            else if(type == VariableType.Drawing)
            {
                foreach (XmlNode typeNode in typeNodeList)
                {
                    if (typeNode.Name == "Image" || typeNode.Name == "Region" || typeNode.Name == "Fixture" || typeNode.Name == "Line2D")
                    {
                        if(typeNode.HasChildNodes)
                        {
                            foreach (XmlNode doubleValue in typeNode.ChildNodes)
                            {
                                if (doubleValue.Attributes.GetNamedItem("enable").Value == "1")
                                {
                                    outputList.Add(typeNode.Name+"@"+doubleValue.InnerText);

                                }
                            }
                        }
                        
                    }
                }
            }

            
            return outputList;
        }

        public int getFrameNumber()
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNode mainNode = xmlDoc.SelectSingleNode("EasyRangerEnv");
            XmlNode varNode = mainNode.ChildNodes.Item(1);
            string numStr = varNode.InnerText;
            return Convert.ToInt32(numStr);
        }




    }
}
