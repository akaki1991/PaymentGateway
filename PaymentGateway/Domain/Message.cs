namespace PaymentGateway.Domain;

public class Message
{
    public Message()
    {
        Id = Guid.NewGuid();
        CratedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; set; }
    /// <summary>
    /// Content of the message in accii format
    /// </summary>
    public string? Content { get; set; }
    public SendStatus SendStatus { get; set; }
    public DateTimeOffset CratedAt { get; set; }
}
