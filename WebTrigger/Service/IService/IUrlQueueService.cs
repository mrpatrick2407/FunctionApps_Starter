public interface IUrlQueueService {
    /// <summary>
    /// Sends a message to the queue asynchronously.
    /// </summary>
    /// <param name="message">The message to be sent to the queue.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendMessageAsync(string message);
}
public interface INotificationQueueService  {
    /// <summary>
    /// Sends a message to the queue asynchronously.
    /// </summary>
    /// <param name="message">The message to be sent to the queue.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendMessageAsync(string message);
}
