namespace LambdaContainer.Core.TestResources.DependencyCycles
{
    public class CycleLvl1
    {
        public CycleLvl1(CycleLvl2 lvl2, CycleLvl3 lvl3)
        {
        }
    }

    public class CycleLvl2
    {

    }

    public class CycleLvl3
    {
        public CycleLvl3(CycleLvl1 lvl1)
        {

        }
    }
}
