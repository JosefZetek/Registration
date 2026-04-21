using System.Linq;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.FeatureComputers;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace DataView
{
    public class CompoundFeatureComputer : AFeatureComputer
    {
        private AFeatureComputer[] featureComputers;
        private int numberOfFeatures;


        public CompoundFeatureComputer(AFeatureComputer[] featureComputers)
        {
            this.featureComputers = featureComputers;
            this.numberOfFeatures = featureComputers.Sum(fc => fc.NumberOfFeatures);
        }

        public override int NumberOfFeatures => numberOfFeatures;

        public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
        {
            int currentIndex = startIndex;

            CheckArrayDimensions(array, startIndex);

            for(int i = 0; i<featureComputers.Length; i++)
            {
                featureComputers[i].ComputeFeatureVector(d, p, array, currentIndex);
                currentIndex += featureComputers[i].NumberOfFeatures;
            }
        }

        public override void GetWeights(double[] array, int startIndex)
        {
            int currentIndex = startIndex;

            CheckArrayDimensions(array, startIndex);

            for(int i = 0; i<featureComputers.Length; i++)
            {
                featureComputers[i].GetWeights(array, currentIndex);
                currentIndex += featureComputers[i].NumberOfFeatures;
            }
        }

        public override FCConfiguration GetConfiguration()
        {
            throw new System.NotImplementedException("This method does not make sense for CompoundFeatureComputer, thus is not implemented.");
        }
        
        public RootConfigurationElement GetRootConfigurationElement()
        {
            var rootConfig = new RootConfigurationElement();
            foreach (var featureComputer in featureComputers)
                rootConfig.configurations.Add(featureComputer.GetConfiguration());
            
            return rootConfig;
        }
    }
}
