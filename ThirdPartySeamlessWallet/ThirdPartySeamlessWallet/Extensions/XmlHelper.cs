using System.Xml;
using System.Xml.Serialization;

namespace ThirdPartySeamlessWallet.Extensions
{
    public static class XmlHelper
    {
        /// <summary>
        /// 自訂 XmlSerializerNamespaces，讓 XML 不會產生 xmlns:xsi 與 xmlns:xsd 屬性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string SerializeToXmlString<T>(T data)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var xmlSerializer = new XmlSerializer(typeof(T));

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);
            xmlSerializer.Serialize(xmlWriter, data, emptyNamespaces);
            return stringWriter.ToString();
        }
    }
}
