namespace Flexler.UITests;

internal sealed record UiTarget(string Id, bool HasLandscape = true, string? AndroidTextLabel = null)
{
    public string For(bool landscape) => Id + (landscape && HasLandscape ? "_Landscape" : "");
    public override string ToString() => Id;
}

internal static class UiTargets
{
    public static readonly UiTarget HostWitness = new("DapperDan_Action_Witness", false);
    public static readonly UiTarget HostOpenFlexler = new("DapperDan_Canary_OpenFlexler", false);
    public static readonly UiTarget ShowcaseBack = new("Flexler_Showcase_Back_Button");
    public static readonly UiTarget Add = new("Flexler_AddItem_Button", false);
    public static readonly UiTarget Count = new("Flexler_ItemCount_Label", false);
    public static readonly UiTarget Summary = new("Flexler_LayoutSummary_Label", false);
    public static readonly UiTarget Empty = new("Flexler_EmptyRecipe_Label", false);
    public static readonly UiTarget Container = new("Flexler_OpenContainerControls_Button");
    public static readonly UiTarget Reset = new("Flexler_Reset_Button");
    public static readonly UiTarget ContainerReset = new("Flexler_ContainerReset_Button");
    public static readonly UiTarget ContainerDone = new("Flexler_CloseContainerControls_Button");
    public static readonly UiTarget ItemText = new("Flexler_ItemText_Entry", AndroidTextLabel: "Selected item text");
    public static readonly UiTarget ItemDone = new("Flexler_CloseInspector_Button");
    public static readonly UiTarget RemoveItem = new("Flexler_RemoveItem_Button");
    public static readonly UiTarget Favorites = new("Flexler_Favorites_Button");
    public static readonly UiTarget FavoriteName = new("Flexler_FavoriteName", AndroidTextLabel: "New favorite name");
    public static readonly UiTarget Save = new("Flexler_SaveFavorite_Button");
    public static readonly UiTarget Saved = new("Flexler_FavoritesPicker");
    public static readonly UiTarget Load = new("Flexler_LoadFavorite_Button");
    public static readonly UiTarget Delete = new("Flexler_DeleteFavorite_Button");
    public static readonly UiTarget FavoritesStatus = new("Flexler_FavoritesStatus");
    public static readonly UiTarget Preview = new("Flexler_PreviewRecipe_Button");
    public static readonly UiTarget FavoritePreview = new("Flexler_FavoriteViewXaml_Button");
    public static readonly UiTarget Recipe = new("Flexler_RecipeXaml");
    public static readonly UiTarget RecipeDone = new("Flexler_CloseRecipe_Button");
    public static readonly UiTarget Copy = new("Flexler_CopyPreview_Button");
    public static readonly UiTarget PreviewStatus = new("Flexler_PreviewStatus");
    public static readonly UiTarget ExportStatus = new("Flexler_ExportStatus_Label");
    public static UiTarget Item(int id) => new($"Flexler_Item_{id}", false);
    public static UiTarget Picker(string property) => new($"Flexler_{property}_Picker");
}
