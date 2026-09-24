using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IDocumentRepository : IRepository<ManagedDocument, DocumentId>
{
}
