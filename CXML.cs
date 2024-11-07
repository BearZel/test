using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace AbakConfigurator
{
    public class CXML
    {
        public static XmlNode getRootNode(XmlDocument document)
        {
            return document.SelectSingleNode("descendant::root");
        }

        public static XPathNavigator getRootNode(XPathNavigator document)
        {
            return document.SelectSingleNode("root");
        }

        public static XmlNode getRootNode(XmlDocument document, String rootName)
        {
            return document.SelectSingleNode("descendant::" + rootName);
        }

        public static String getNodeValue(XPathNavigator node, String name)
        {
            XPathNavigator n = node.SelectSingleNode("descendant::" + name);
            if (n != null)
                return n.Value;
            else
                return "";
        }

        public static String getNodeValue(XPathNavigator node, String name, string defVal)
        {
            String res = CXML.getNodeValue(node, name);
            if (res == "")
                res = defVal;

            return res;
        }

        //Функция получает указатель ветви устройства
        public static XmlNode getDeviceNode(XmlDocument document)
        {
            try
            {
                XmlNode root = getRootNode(document);
                return root.ChildNodes[0];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static XPathNavigator getDeviceNode(XPathNavigator document)
        {
            return getRootNode(document).SelectSingleNode("descendant::device"); 
        }

        public static XmlNode getNodeByName(XmlNode node, String name)
        {
            return node.SelectSingleNode("descendant::" + name);
        }

        public static XmlNodeList getNodesByName(XmlNode node, String name)
        {
            return node.SelectNodes("descendant::" + name);
        }

        /**
         * Функция добавляет атрибут в узел
         */
        public static void appendAttributeToNode(XmlNode node, String Name, String Value)
        {
            XmlAttribute att = node.OwnerDocument.CreateAttribute(Name);
            att.Value = Value;
            node.Attributes.Append(att);
        }

        /**
         * Функция возвращает запрашиваемый атрибут
         */
        public static string getAttributeValue(XmlNode node, string attrName, string defVal)
        {
            XmlNode attr = node.Attributes[attrName];
            if (attr != null)
                return attr.Value;
            else
                return defVal;
        }

        public static string getAttributeValue(XPathNavigator node, string attrName, string defVal)
        {
            String attr = node.GetAttribute(attrName, "");
            if(attr == null)
                return defVal;

            if (attr != String.Empty)
                return attr;
            else
                return defVal;
        }

        public static bool attributeExists(XPathNavigator node, string attrName)
        {
            XPathNavigator node2 = node.Clone();
            return node2.MoveToAttribute(attrName, "");
        }

        public static void SetAttributeValue(XmlDocument doc, XmlNode ruleNode, string attrName, string value)
        {
            XmlAttribute attr = doc.CreateAttribute(attrName);
            attr.Value = value;
            ruleNode.Attributes.Append(attr);
        }
    }
}
