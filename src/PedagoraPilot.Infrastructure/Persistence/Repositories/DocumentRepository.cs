using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class DocumentRepository(PedagoraPilotDbContext db) : Repository<ManagedDocument, DocumentId>(db), IDocumentRepository;
