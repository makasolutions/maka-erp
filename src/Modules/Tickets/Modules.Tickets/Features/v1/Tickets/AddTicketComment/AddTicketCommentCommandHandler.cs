using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using FSH.Modules.Tickets.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Features.v1.Tickets.AddTicketComment;

public sealed class AddTicketCommentCommandHandler(
    TicketsDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<AddTicketCommentCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddTicketCommentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var authorId = currentUser.GetUserId();
        if (authorId == Guid.Empty)
        {
            throw new CustomException(
                "Cannot post a comment without an authenticated author.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Unauthorized);
        }

        // Loading the Comments collection up front gives EF's change tracker
        // a populated nav property to compare against, so adding a new
        // TicketComment via the aggregate's encapsulated method is detected
        // as an INSERT rather than slipping through change detection.
        var ticket = await dbContext.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket {command.TicketId} not found.");

        var comment = ticket.AddComment(authorId, command.Body);
        // Register the new comment with the DbSet so EF tracks it as Added.
        // The aggregate also holds it in its Comments navigation, but the
        // client-generated Guid key makes EF mis-detect a navigation-only add
        // as Modified (→ UPDATE 0 rows → DbUpdateConcurrencyException).
        await dbContext.TicketComments.AddAsync(comment, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return comment.Id;
    }
}
