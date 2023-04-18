using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

partial class MstatData
{
    private Dictionary<string, Node> _nameToNode;

    public Node GetNodeForId(int id)
    {
        if (_version.Major < 2 || _nameToNode == null)
            return null;

        PEMemoryBlock nameMap = _peReader.GetSectionData(".names");
        BlobReader nameMapReader = nameMap.GetReader();
        nameMapReader.Offset = id;
        string name = nameMapReader.ReadSerializedString();
        return _nameToNode.GetValueOrDefault(name);
    }

    private void TryLoadAssociatedDgmlFile(string fileName)
    {
        fileName = Path.ChangeExtension(fileName, "scan.dgml.xml");

        if (!File.Exists(fileName))
            return;

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
