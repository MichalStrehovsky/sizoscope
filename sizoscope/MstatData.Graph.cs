using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;
using TurboXml;

partial class MstatData
{
    private Dictionary<string, Node> _nameToNode;

    public bool DgmlSupported => _version.Major >= 2;
    public bool DgmlAvailable => DgmlSupported && _nameToNode != null;

    private string GetNameForId(int id)
    {
        PEMemoryBlock nameMap = _peReader.GetSectionData(".names");
        BlobReader nameMapReader = nameMap.GetReader();
        nameMapReader.Offset = id;
        return nameMapReader.ReadSerializedString();
    }

    public Node GetNodeForId(int id, out string name)
    {
        name = GetNameForId(id);
        return _nameToNode.GetValueOrDefault(name);
    }

    struct Reader : IXmlReadHandler
    {
        enum ReadMode { None, Nodes, Links }

        private readonly Dictionary<string, Node> _nameToNode = new Dictionary<string, Node>(StringComparer.Ordinal);
        private readonly Dictionary<int, Node> _idToNode = new Dictionary<int, Node>();

        private ReadMode _readMode;

        private int _id;
        private string _name;

        private int _source;
        private int _target;
        private string _reason;

        public Reader() { }

        public Dictionary<string, Node> ToDictionary() => _nameToNode;

        void IXmlReadHandler.OnAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int nameLine, int nameColumn, int valueLine, int valueColumn)
        {
            if (_readMode == ReadMode.Links)
            {
                if (name.Equals("Source", StringComparison.Ordinal))
                    _source = int.Parse(value);
                else if (name.Equals("Target", StringComparison.Ordinal))
                    _target = int.Parse(value);
                else if (name.Equals("Reason", StringComparison.Ordinal))
                    _reason = new string(value);
            }
            else if (_readMode == ReadMode.Nodes)
            {
                if (name.Equals("Id", StringComparison.Ordinal))
                    _id = int.Parse(value);
                else if (name.Equals("Label", StringComparison.Ordinal))
                    _name = new string(value);
            }
        }

        void IXmlReadHandler.OnEndTagEmpty()
        {
            if (_readMode == ReadMode.Links)
            {
                _idToNode[_target].Edges.Add((_idToNode[_source], _reason));
            }
            else if (_readMode == ReadMode.Nodes)
            {
                var n = new Node(_name);
                _idToNode[_id] = n;
                _nameToNode[_name] = n;
            }
        }

        void IXmlReadHandler.OnEndTag(ReadOnlySpan<char> name, int line, int column)
        {
            if (name.Equals("Nodes", StringComparison.Ordinal) || name.Equals("Links", StringComparison.Ordinal))
            {
                _readMode = ReadMode.None;
            }
        }

        void IXmlReadHandler.OnBeginTag(ReadOnlySpan<char> name, int line, int column)
        {
            if (_readMode == ReadMode.None)
            {
                if (name.Equals("Nodes", StringComparison.Ordinal))
                    _readMode = ReadMode.Nodes;
                else if (name.Equals("Links", StringComparison.Ordinal))
                    _readMode = ReadMode.Links;
            }
        }

        void IXmlReadHandler.OnCData(ReadOnlySpan<char> cdata, int line, int column) { }
        void IXmlReadHandler.OnComment(ReadOnlySpan<char> comment, int line, int column) { }
        void IXmlReadHandler.OnText(ReadOnlySpan<char> text, int line, int column) { }

        void IXmlReadHandler.OnXmlDeclaration(ReadOnlySpan<char> version, ReadOnlySpan<char> encoding, ReadOnlySpan<char> standalone, int line, int column)
        { }
    }

    private void TryLoadAssociatedDgmlFile(string fileName)
    {
        fileName = Path.ChangeExtension(fileName, "scan.dgml.xml");

        if (!File.Exists(fileName))
            return;

#if true
        using FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        var handler = new Reader();
        XmlParser.Parse(fs, ref handler);
        _nameToNode = handler.ToDictionary();
#elif false
        var idToNode = new Dictionary<int, Node>();
        _nameToNode = new Dictionary<string, Node>(StringComparer.Ordinal);
        using (XmlReader reader = XmlReader.Create(fileName))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Node")
                {
                    int id = 0;
                    string name = null;
                    bool cont = reader.MoveToFirstAttribute();
                    while (cont)
                    {
                        string attributeName = reader.Name;
                        if (attributeName == "Id")
                            id = int.Parse(reader.Value);
                        if (attributeName == "Label")
                            name = reader.Value;
                        cont = reader.MoveToNextAttribute();
                    }

                    var n = new Node(name);
                    idToNode[id] = n;
                    _nameToNode[name] = n;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Link")
                {
                    int source = 0;
                    int target = 0;
                    string reason = null;
                    bool cont = reader.MoveToFirstAttribute();
                    while (cont)
                    {
                        string attributeName = reader.Name;
                        if (attributeName == "Source")
                            source = int.Parse(reader.Value);
                        if (attributeName == "Target")
                            target = int.Parse(reader.Value);
                        if (attributeName == "Reason")
                            reason = reader.Value;
                        cont = reader.MoveToNextAttribute();
                    }

                    idToNode[target].Edges.Add((idToNode[source], reason));
                }
            }
        }
#else
        var directedGraph = XElement.Load(fileName);

        var idToNode = new Dictionary<int, Node>();
        _nameToNode = new Dictionary<string, Node>(StringComparer.Ordinal);
        var nodes = directedGraph.Elements().Single(e => e.Name.LocalName == "Nodes");
        foreach (var node in nodes.Elements())
        {
            Debug.Assert(node.Name.LocalName == "Node");
            int id = int.Parse(node.Attribute("Id").Value);
            string name = node.Attribute("Label").Value;
            var n = new Node(name);
            idToNode[id] = n;
            _nameToNode[name] = n;
        }

        var links = directedGraph.Elements().Single(e => e.Name.LocalName == "Links");
        foreach (var link in links.Elements())
        {
            int source = int.Parse(link.Attribute("Source").Value);
            int target = int.Parse(link.Attribute("Target").Value);
            string reason = link.Attribute("Reason").Value;
            idToNode[target].Edges.Add((idToNode[source], reason));
        }
#endif
    }


    public class Node
    {
        public readonly string Name;
        public readonly List<(Node Node, string Label)> Edges;

        public Node(string name)
        {
            Name = name;
            Edges = new List<(Node, string)>();
        }
    }
}
