using Tolk.BusinessLogic.Utilities;
namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    public enum TestChild
    {
        [Parent(TestParent.ParentOne)]
        ChildOne = 1,
        [Parent(TestParent.ParentOne)]
        ChildTwo = 2,
        [Parent(TestParent.ParentTwo)]
        ChildThree = 3,
        [Parent(TestParent.ParentTwo)]
        ChildFour = 4,
        [Parent(TestParent.ParentTwo)]
        ChildFive = 5,
        [Parent(TestParent.ParentTwo)]
        ChildSix = 6,
        [Parent(TestParent.ParentThree)]
        ChildSeven = 7
    }
}
