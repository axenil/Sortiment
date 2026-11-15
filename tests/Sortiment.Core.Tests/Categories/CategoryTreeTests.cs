using Sortiment.Core.Categories;
using Sortiment.Core.Tests.Fixtures;

namespace Sortiment.Core.Tests.Categories;

public sealed class CategoryTreeTests
{
    private readonly TShirtCatalog _catalog = new();

    private CategoryTree Tree => _catalog.WebShop;

    [Fact]
    public void CreateReturnsAnEmptyTree()
    {
        var tree = CategoryTree.Create(CategoryTreeCode.Parse("print_catalog")).Value;

        tree.Code.Value.ShouldBe("print_catalog");
        tree.Categories.ShouldBeEmpty();
        tree.Children(null).ShouldBeEmpty();
    }

    [Fact]
    public void AddCategoryPlacesItLastUnderItsParent()
    {
        var cotton = Tree.AddCategory(Code("cotton_t_shirts"), Code("t_shirts")).Value;

        cotton.ParentId.ShouldBe(_catalog.TShirtCategory.Id);
        Tree.Children(_catalog.TShirtCategory.Id).ShouldBe([_catalog.LinenCategory, cotton]);
        Tree.FindCategory(cotton.Id).ShouldBe(cotton);
        Tree.FindCategory(Code("cotton_t_shirts")).ShouldBe(cotton);
    }

    [Fact]
    public void CategoriesAreListedDepthFirstInSiblingOrder()
    {
        Tree.Categories.Select(category => category.Code.Value)
            .ShouldBe(["clothing", "t_shirts", "linen_t_shirts", "summer"]);
        Tree.Children(null).ShouldBe([_catalog.Clothing, _catalog.Summer]);
    }

    [Fact]
    public void AddCategoryReportsADuplicateCodeAndAnUnknownParent()
    {
        var errors = Tree.AddCategory(Code("summer"), Code("winter")).Errors;

        errors.Select(error => (error.Code, error.Context[ErrorContextKeys.Category])).ShouldBe(
        [
            ("category_tree.duplicate_category", "summer"),
            ("category_tree.category_not_found", "winter"),
        ]);
        errors.ShouldAllBe(error => error.Context[ErrorContextKeys.CategoryTree] == "web_shop");
        Tree.Categories.Count.ShouldBe(4);
    }

    [Fact]
    public void MoveCategoryMovesItsSubtreeAndKeepsItsId()
    {
        Tree.MoveCategory(Code("t_shirts"), Code("summer"), 0).ShouldSucceed();

        var moved = Tree.FindCategory(Code("t_shirts"))!;
        moved.Id.ShouldBe(_catalog.TShirtCategory.Id);
        moved.ParentId.ShouldBe(_catalog.Summer.Id);
        Tree.Children(_catalog.Clothing.Id).ShouldBeEmpty();
        Tree.Children(_catalog.Summer.Id).ShouldBe([moved]);
        Tree.FindCategory(Code("linen_t_shirts"))!.ParentId.ShouldBe(moved.Id);
        Tree.Categories.Select(category => category.Code.Value)
            .ShouldBe(["clothing", "summer", "t_shirts", "linen_t_shirts"]);
    }

    [Fact]
    public void MoveCategoryCanMoveACategoryToTheTopLevel()
    {
        Tree.MoveCategory(Code("linen_t_shirts"), null, 1).ShouldSucceed();

        Tree.Children(null).Select(category => category.Code.Value).ShouldBe(["clothing", "linen_t_shirts", "summer"]);
        Tree.FindCategory(Code("linen_t_shirts"))!.ParentId.ShouldBeNull();
    }

    [Fact]
    public void MoveCategoryReordersSiblings()
    {
        Tree.MoveCategory(Code("summer"), null, 0).ShouldSucceed();

        Tree.Children(null).ShouldBe([_catalog.Summer, _catalog.Clothing]);
    }

    [Theory]
    [InlineData("clothing")]
    [InlineData("linen_t_shirts")]
    public void MoveCategoryRefusesToMoveACategoryIntoItsOwnSubtree(string parent)
    {
        var error = Tree.MoveCategory(Code("clothing"), Code(parent), 0).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("category_tree.move_into_own_subtree");
        error.Context[ErrorContextKeys.Category].ShouldBe("clothing");
        Tree.FindCategory(Code("clothing"))!.ParentId.ShouldBeNull();
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(2, 1)]
    public void MoveCategoryRejectsAPositionOutsideTheSiblings(int position, int maxPosition)
    {
        var error = Tree.MoveCategory(Code("summer"), null, position).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("category_tree.invalid_position");
        error.Message.ShouldContain($"between 0 and {maxPosition}");
        Tree.Children(null).ShouldBe([_catalog.Clothing, _catalog.Summer]);
    }

    [Fact]
    public void MoveCategoryReportsUnknownCategories()
    {
        Tree.MoveCategory(Code("winter"), Code("autumn"), 0).Errors
            .Select(error => error.Context[ErrorContextKeys.Category])
            .ShouldBe(["winter", "autumn"]);
    }

    [Fact]
    public void RemoveCategoryRemovesALeafCategory()
    {
        var removed = Tree.RemoveCategory(Code("linen_t_shirts")).Value;

        removed.ShouldBe(_catalog.LinenCategory);
        Tree.FindCategory(Code("linen_t_shirts")).ShouldBeNull();
        Tree.FindCategory(removed.Id).ShouldBeNull();
        Tree.Children(_catalog.TShirtCategory.Id).ShouldBeEmpty();
        Tree.AddCategory(Code("linen_t_shirts"), null).ShouldSucceed();
    }

    [Fact]
    public void RemoveCategoryRefusesACategoryWithSubcategories()
    {
        var error = Tree.RemoveCategory(Code("t_shirts")).Errors.ShouldHaveSingleItem();

        error.Code.ShouldBe("category_tree.category_has_children");
        error.Context[ErrorContextKeys.Category].ShouldBe("t_shirts");
        Tree.Categories.Count.ShouldBe(4);
    }

    [Fact]
    public void RemoveCategoryReportsAnUnknownCategory()
    {
        Tree.RemoveCategory(Code("winter")).Errors.ShouldHaveSingleItem().Code
            .ShouldBe("category_tree.category_not_found");
    }

    [Fact]
    public void ChildrenRejectsACategoryOutsideTheTree()
    {
        Should.Throw<ArgumentException>(() => Tree.Children(CategoryId.New()));
    }

    [Fact]
    public void RestoreKeepsTheStructureAndSiblingOrder()
    {
        var clothing = new Category(CategoryId.New(), Code("clothing"), null);
        var shirts = new Category(CategoryId.New(), Code("shirts"), clothing.Id);
        var trousers = new Category(CategoryId.New(), Code("trousers"), clothing.Id);
        var id = CategoryTreeId.New();

        var tree = CategoryTree.Restore(id, CategoryTreeCode.Parse("web_shop"), [trousers, shirts, clothing]);

        tree.Id.ShouldBe(id);
        tree.Children(null).ShouldBe([clothing]);
        tree.Children(clothing.Id).ShouldBe([trousers, shirts]);
    }

    [Fact]
    public void RestoreReportsEveryStructuralProblem()
    {
        var first = new Category(CategoryId.New(), Code("first"), null);
        var loopBId = CategoryId.New();
        var loopA = new Category(CategoryId.New(), Code("loop_a"), loopBId);
        var loopB = new Category(loopBId, Code("loop_b"), loopA.Id);
        var self = CategoryId.New();
        var orphan = CategoryId.New();
        Category[] categories =
        [
            first,
            new(first.Id, Code("duplicate_id"), null),
            new(CategoryId.New(), Code("first"), null),
            loopA,
            loopB,
            new(CategoryId.New(), Code("below_loop"), loopA.Id),
            new(self, Code("self"), self),
            new(CategoryId.New(), Code("orphan"), orphan),
        ];

        var exception = Should.Throw<ArgumentException>(() =>
            CategoryTree.Restore(CategoryTreeId.New(), CategoryTreeCode.Parse("web_shop"), categories));

        exception.ParamName.ShouldBe("categories");
        exception.Message.ShouldContain($"Category ID {first.Id} is used more than once.");
        exception.Message.ShouldContain("Category code 'first' is used more than once.");
        exception.Message.ShouldContain("Category 'loop_a' is its own ancestor.");
        exception.Message.ShouldContain("Category 'loop_b' is its own ancestor.");
        exception.Message.ShouldNotContain("'below_loop'");
        exception.Message.ShouldContain("Category 'self' is its own ancestor.");
        exception.Message.ShouldContain($"The parent {orphan} of category 'orphan' is not in the tree.");
    }

    private static CategoryCode Code(string value) => CategoryCode.Parse(value);
}
