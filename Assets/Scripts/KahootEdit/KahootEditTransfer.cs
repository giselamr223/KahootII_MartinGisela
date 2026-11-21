public static class KahootEditTransfer
{
    public static KahootData KahootToEdit { get; private set; }
    public static string FileName { get; private set; }
    public static bool HasKahootToEdit => KahootToEdit != null;

    public static void SetForEdit(KahootData data, string fileName)
    {
        KahootToEdit = data;
        FileName = fileName;
    }

    public static void Clear()
    {
        KahootToEdit = null;
        FileName = null;
    }
}