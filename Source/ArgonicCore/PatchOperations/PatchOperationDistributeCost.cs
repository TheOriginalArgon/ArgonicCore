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
        protected int percentage;
        protected float extraCostFactor = 1f;
        protected int minimum = 1;
        protected bool logging = false;
        protected bool round = true;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = false;

            XmlNode[] selectedNodes = xml.SelectNodes(xpath)?.Cast<XmlNode>().ToArray();
            if (selectedNodes == null || selectedNodes.Length == 0)
                return false;

            foreach (XmlNode node in selectedNodes)
            {
                if (!int.TryParse(node.InnerText, out int originalAmount) || originalAmount <= minimum)
                    continue;

                result = true;

                int splitAmount;
                if (string.IsNullOrEmpty(splitMode))
                    originalAmount = SplitUtility.Split(percentage, extraCostFactor, originalAmount, out splitAmount, round);
                else
                    originalAmount = SplitUtility.Split(splitMode, originalAmount, out splitAmount); // Obsolete. Kept for compatibility with old patches. Will be removed in the future.

                XmlNode newMaterialNode = node.OwnerDocument.CreateElement(newMaterial);
                newMaterialNode.InnerText = splitAmount.ToString();

                // Insert the new material node before the current node. If the original amount
                // drops to zero the old node will be removed, otherwise we update its value.
                node.ParentNode.InsertBefore(newMaterialNode, node);

                if (originalAmount > 0)
                {
                    node.InnerText = originalAmount.ToString();      
                }
                else
                {
                    node.ParentNode.RemoveChild(node);
                }

                if (logging)
                {
                    Log.Warning($"{originalAmount} is what left of {node.Name}");
                    Log.Warning($"{splitAmount} is what was cut and put in {newMaterialNode.Name}");
                }
            }

            return result;
        }
    }
}
