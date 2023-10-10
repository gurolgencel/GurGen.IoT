using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using GurGen.IoT.Activities.Activities.OPCUA;
using GurGen.IoT.Activities.Design.Designers;
using GurGen.IoT.Activities.Design.Properties;

namespace GurGen.IoT.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");
            //var opcuaCategoryAttribute = new CategoryAttribute("OPCUA");
            
            builder.AddCustomAttributes(typeof(OPCUAReadSingleNode), categoryAttribute);
            builder.AddCustomAttributes(typeof(OPCUAReadSingleNode), new DesignerAttribute(typeof(OPCUAReadSingleNodeDesigner)));
            builder.AddCustomAttributes(typeof(OPCUAReadSingleNode), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}
