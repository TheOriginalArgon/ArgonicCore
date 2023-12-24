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

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = false;

            XmlNode[] selectedNodes = xml.SelectNodes(xpath).Cast<XmlNode>().ToArray();

            foreach (XmlNode node in selectedNodes)
            {
                if (Convert.ToInt32(node.InnerText) > minimum)
                {
                    result = true;
                    int originalAmount = Convert.ToInt32(node.InnerText);
                    originalAmount = SplitUtility.Split(splitMode, originalAmount, out int splitAmount);
                    XmlNode newMaterialNode = node.OwnerDocument.CreateElement(newMaterial);
                    newMaterialNode.InnerXml = node.InnerXml;
                    newMaterialNode.InnerText = splitAmount.ToString();
                    node.InnerText = originalAmount.ToString();
                    node.ParentNode.InsertBefore(newMaterialNode, node);
                }
            }
            return result;
        }
    }
}
