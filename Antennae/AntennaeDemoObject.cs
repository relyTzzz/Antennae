using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using Grasshopper.Kernel.Special;

namespace Antennae
{
    public class AntennaeDemoObject : GH_Component
    {
        public AntennaeDemoObject()
            : base("AntennaeDemoObject", "Demo Object",
                "Instantiates a specific cluster. You must explode the cluster before adding a new instance of this object.",
                "Antennae", "Tools")
        {
        }

        public override Guid ComponentGuid
        {
            // get { return Guid.NewGuid(); }
            get { return new Guid("84a347a4-c075-4310-af8c-0544a0cfddd6"); }
        }

        protected override Bitmap Icon
        {
            get
            {
                // The name of the embedded resource must be the full default namespace + the file name of the icon
                var resourceName = "Antennae.Resources.Icons.demo_icon.png";
                // Retrieve the icon from the embedded resources
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        return new Bitmap(stream);
                    }
                }
                return null; // Alternatively, return a default icon
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // Your input params
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            // Your output params
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // If this component doesn't do anything else than instantiate the cluster,
            // you might not need to implement anything here.
        }

        // This method will list all embedded resources
        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            foreach (var resource in resources)
            {
                Rhino.RhinoApp.WriteLine("Resource: " + resource);
            }
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            InstantiateCluster(document);

            // This removes the component itself after the cluster is added
            document.RemoveObject(this.Attributes, false);
        }


        private void InstantiateCluster(GH_Document document)
        {
            // Find the embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Antennae.Resources.Clusters.demo_object.ghcluster";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The .ghcluster stream could not be found.");
                    return;
                }

                // Read the stream into a byte array
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                var archive = new GH_Archive();
                if (!archive.Deserialize_Binary(buffer))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Deserialization of .ghcluster failed.");
                    return;
                }

                // Obtain the chunk containing the cluster information
                var rootChunk = archive.GetRootNode;
                if (rootChunk == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The .ghcluster does not contain a root chunk.");
                    return;
                }

                // Create a new cluster and read the chunk
                var cluster = new GH_Cluster();
                if (cluster.Read(rootChunk))
                {
                    // Check if a cluster with the same ID already exists in the document
                    var existingCluster = document.Objects.FirstOrDefault(obj => obj is GH_Cluster && ((GH_Cluster)obj).InstanceGuid == cluster.InstanceGuid);
                    if (existingCluster != null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A cluster with the same ID already exists in the document.");
                        return;
                    }

                    // If no existing cluster is found, add the new cluster to the document
                    document.AddObject(cluster, false);

                    // Optionally, set the attributes such as position
                    cluster.Attributes.Pivot = this.Attributes.Pivot;
                    cluster.Attributes.ExpireLayout();
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to create the cluster from the chunk.");
                }
            }
        }

    }
}
