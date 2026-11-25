using Sortiment.Core.Categories;
using Sortiment.Core.Products;
using Sortiment.Core.Tests.Fixtures;

namespace Sortiment.Core.Tests.Products;

public sealed class ProductCategoryTests
{
    private readonly TShirtCatalog _catalog = new();
    private readonly Product _product;
    private readonly ProductModel _model;

    public ProductCategoryTests()
    {
        _product = Product.Create(Sku.Parse("TS-001"), _catalog.TShirtSchema()).Value;
        _model = ProductModel.Create(ProductModelCode.Parse("linen_shirt"), _catalog.ColorAndSizeSchema()).Value;
    }

    [Fact]
    public void NewProductsAndModelsAreInNoCategories()
    {
        _product.Categories.ShouldBeEmpty();
        _model.Categories.ShouldBeEmpty();
    }

    [Fact]
    public void AddCategoryResolvesTheCategoryByCodeAtAnyLevel()
    {
        _product.AddCategory(_catalog.WebShop, Code("linen_t_shirts")).ShouldSucceed();
        _product.AddCategory(_catalog.WebShop, Code("clothing")).ShouldSucceed();
        _model.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();

        _product.Categories.ShouldBe([_catalog.LinenCategory.Id, _catalog.Clothing.Id]);
        _model.Categories.ShouldBe([_catalog.Summer.Id]);
    }

    [Fact]
    public void AddCategoryAcceptsCategoriesFromSeveralTrees()
    {
        var print = CategoryTree.Create(CategoryTreeCode.Parse("print_catalog")).Value;
        var chapter = print.AddCategory(Code("summer_chapter"), null).Value;

        _product.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();
        _product.AddCategory(print, Code("summer_chapter")).ShouldSucceed();

        _product.Categories.ShouldBe([_catalog.Summer.Id, chapter.Id]);
    }

    [Fact]
    public void AddingACategoryTwiceChangesNothing()
    {
        _product.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();
        _product.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();

        _product.Categories.ShouldBe([_catalog.Summer.Id]);
    }

    [Fact]
    public void AddCategoryReportsACategoryThatIsNotInTheTree()
    {
        var productError = _product.AddCategory(_catalog.WebShop, Code("winter")).Errors.ShouldHaveSingleItem();
        var modelError = _model.AddCategory(_catalog.WebShop, Code("winter")).Errors.ShouldHaveSingleItem();

        productError.Code.ShouldBe("product.category_not_found");
        productError.Context[ErrorContextKeys.Product].ShouldBe("TS-001");
        productError.Context[ErrorContextKeys.CategoryTree].ShouldBe("web_shop");
        productError.Context[ErrorContextKeys.Category].ShouldBe("winter");
        modelError.Code.ShouldBe("product_model.category_not_found");
        modelError.Context[ErrorContextKeys.ProductModel].ShouldBe("linen_shirt");
        _product.Categories.ShouldBeEmpty();
        _model.Categories.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveCategoryTakesTheProductOutOfIt()
    {
        _product.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();
        _model.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();

        _product.RemoveCategory(_catalog.Summer.Id).ShouldBeTrue();
        _product.RemoveCategory(_catalog.Summer.Id).ShouldBeFalse();
        _model.RemoveCategory(_catalog.Summer.Id).ShouldBeTrue();
        _product.Categories.ShouldBeEmpty();
        _model.Categories.ShouldBeEmpty();
    }

    [Fact]
    public void ProductsStayInACategoryThatIsMoved()
    {
        _product.AddCategory(_catalog.WebShop, Code("linen_t_shirts")).ShouldSucceed();

        _catalog.WebShop.MoveCategory(Code("linen_t_shirts"), Code("summer"), 0).ShouldSucceed();

        _product.Categories.ShouldBe([_catalog.LinenCategory.Id]);
    }

    [Fact]
    public void CategoriesDoNotAffectValidationOrCompleteness()
    {
        var schema = _catalog.TShirtSchema();
        var before = MissingAttributes(_product.CalculateCompleteness(schema, _catalog.Ecommerce));

        _product.AddCategory(_catalog.WebShop, Code("summer")).ShouldSucceed();

        _product.Validate(schema).ShouldSucceed();
        MissingAttributes(_product.CalculateCompleteness(schema, _catalog.Ecommerce)).ShouldBe(before);
    }

    [Fact]
    public void RestoreKeepsTheCategories()
    {
        CategoryId[] categories = [_catalog.Summer.Id, _catalog.Clothing.Id];

        var product = Product.Restore(
            ProductId.New(),
            Sku.Parse("TS-001"),
            _catalog.TShirts.Id,
            isEnabled: true,
            [],
            categories: categories);
        var model = ProductModel.Restore(
            ProductModelId.New(),
            ProductModelCode.Parse("linen_shirt"),
            _catalog.ByColorAndSize.Id,
            [],
            categories);

        product.Categories.ShouldBe(categories);
        model.Categories.ShouldBe(categories);
    }

    [Fact]
    public void RestoreRejectsACategoryListedTwice()
    {
        CategoryId[] categories = [_catalog.Summer.Id, _catalog.Summer.Id];

        var productException = Should.Throw<ArgumentException>(() => Product.Restore(
            ProductId.New(),
            Sku.Parse("TS-001"),
            _catalog.TShirts.Id,
            isEnabled: true,
            [],
            categories: categories));
        var modelException = Should.Throw<ArgumentException>(() => ProductModel.Restore(
            ProductModelId.New(),
            ProductModelCode.Parse("linen_shirt"),
            _catalog.ByColorAndSize.Id,
            [],
            categories));

        productException.ParamName.ShouldBe("categories");
        productException.Message.ShouldContain($"Category {_catalog.Summer.Id} is listed more than once.");
        modelException.ParamName.ShouldBe("categories");
    }

    private static List<string> MissingAttributes(ChannelCompleteness completeness) =>
        completeness.Locales.Select(locale => $"{locale.Locale}: {string.Join(", ", locale.MissingAttributes)}").ToList();

    private static CategoryCode Code(string value) => CategoryCode.Parse(value);
}
