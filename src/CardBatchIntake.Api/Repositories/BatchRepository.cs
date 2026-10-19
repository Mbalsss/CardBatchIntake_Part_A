using CardBatchIntake.Api.Models;

namespace CardBatchIntake.Api.Repositories;

// Describes how the application saves and looks up accepted batches.
public interface IBatchRepository
{
    // Saves a new accepted batch.
    void Add(Batch batch);

    // Updates an existing batch, such as when it is released.
    void Update(Batch batch);

    // Finds one batch by its identifier.
    Batch? Get(Guid id);

    // Returns all batches currently saved.
    List<Batch> List();

    // Checks whether a file name has already been submitted.
    bool FileNameExists(string fileName);
}

// Keeps batches in memory while the application is running.
public sealed class InMemoryBatchRepository : IBatchRepository
{
    private readonly Dictionary<Guid, Batch> _batches = new();
    private readonly object _lock = new();

    public void Add(Batch batch)
    {
        lock (_lock) _batches.Add(batch.Id, batch);
    }

    public void Update(Batch batch)
    {
        lock (_lock) _batches[batch.Id] = batch;
    }

    public Batch? Get(Guid id)
    {
        lock (_lock) return _batches.GetValueOrDefault(id);
    }

    public List<Batch> List()
    {
        lock (_lock) return _batches.Values.ToList();
    }

    public bool FileNameExists(string fileName)
    {
        lock (_lock) return _batches.Values.Any(b => b.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }
}