using ArgonicCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace ArgonicCore.PatchOperations
{
    public class PatchOperationDistributeCost : PatchOperationPathed
    {
        protected string newMaterial;
        protected string splitMode;
        protected int minimum = 1;
        protected bool logging = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = false;

            XmlNode[] selectedNodes = xml.SelectNodes(xpath).Cast<XmlNode>().ToArray();

            foreach (XmlNode node in selectedNodes)
            {
                if (Convert.ToInt32(node.InnerText) > minimum)
                {
                    result = true;
                    bool removeFlag = false;
                    int originalAmount = Convert.ToInt32(node.InnerText);
                    originalAmount = SplitUtility.Split(splitMode, originalAmount, out int splitAmount);
                    XmlNode newMaterialNode = node.OwnerDocument.CreateElement(newMaterial);
                    //newMaterialNode.InnerXml = node.InnerXml;
                    newMaterialNode.InnerText = splitAmount.ToString();
                    if (originalAmount > 0)
                    {
                        node.InnerText = originalAmount.ToString();
                        if (logging)
                        {
                            Log.Warning($"{originalAmount} is what left of {node.Name}");
                        }
                    }
                    else
                    {
                        removeFlag = true;
                    }
                    node.ParentNode.InsertBefore(newMaterialNode, node);
                    if (removeFlag)
                    {
                        node.ParentNode.RemoveChild(node);
                    }
                    if (logging)
                    {
                        Log.Warning($"{splitAmount} is what was cut and put in {newMaterialNode.Name}");
                    }
                }
            }
            return result;
        }
    }
}
