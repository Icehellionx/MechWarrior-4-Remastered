namespace MW4Remastered.Core.Media;

public sealed class MediaInspectionService
{
    private readonly DirectoryMediaInventory inventory;
    private readonly MediaRecognizer recognizer;

    public MediaInspectionService()
        : this(new DirectoryMediaInventory(), new MediaRecognizer())
    {
    }

    public MediaInspectionService(DirectoryMediaInventory inventory, MediaRecognizer recognizer)
    {
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
    }

    public MediaRecognitionResult InspectDirectory(string rootPath)
    {
        return recognizer.Recognize(inventory.Read(rootPath));
    }
}
