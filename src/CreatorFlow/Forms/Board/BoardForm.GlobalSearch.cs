using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Search;
using CreatorFlow.Models;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Forms.Board;

public partial class BoardForm
{
    private async Task SearchGloballyAsync(string query)
    {
        if (_auth is null || _projects is null || IsDisposed) return;

        _globalSearchRequest?.Cancel();
        _globalSearchRequest?.Dispose();
        var request = CancellationTokenSource.CreateLinkedTokenSource(_authLifetime.Token);
        _globalSearchRequest = request;
        int generation = ++_globalSearchGeneration;
        long authGeneration = _auth.Session.Generation;

        try
        {
            GlobalSearchResponse? response = await _auth.SearchGlobalAsync(query, 30, request.Token);
            if (!IsCurrentRequest()) return;
            if (response is null)
            {
                _topHeaderControl.SetGlobalSearchError(query, "Phiên đăng nhập đã kết thúc. Hãy đăng nhập lại.");
                return;
            }
            _topHeaderControl.SetGlobalSearchResults(query, response.Items);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ApiException or ApiBusinessException or InvalidOperationException)
        {
            if (IsCurrentRequest()) _topHeaderControl.SetGlobalSearchError(query, error.Message);
        }
        finally
        {
            if (ReferenceEquals(_globalSearchRequest, request)) _globalSearchRequest = null;
            request.Dispose();
        }

        bool IsCurrentRequest() => !IsDisposed && !request.IsCancellationRequested &&
            generation == _globalSearchGeneration && authGeneration == _auth.Session.Generation;
    }

    private async Task OpenGlobalSearchResultAsync(GlobalSearchItemResponse result)
    {
        _globalSearchRequest?.Cancel();
        _globalSearchGeneration++;
        if (_projects is null || _auth?.Session.IsAuthenticated != true || IsDisposed) return;

        long authGeneration = _auth.Session.Generation;
        long targetProjectId = result.ProjectId;
        if (_projects.Context.Current?.ProjectId != targetProjectId)
        {
            await SelectProjectCoreAsync(targetProjectId, confirmSelection: true);
            if (IsDisposed || !_auth.Session.IsAuthenticated || authGeneration != _auth.Session.Generation ||
                _projects.Context.Current?.ProjectId != targetProjectId) return;
        }
        else if (!CanLeaveProjectPage())
        {
            return;
        }

        switch (result.Type)
        {
            case "Project":
                CloseProjectPage();
                CloseProfile();
                CloseAdmin();
                CloseReports();
                CloseAdminPlans();
                CloseAdminStatistics();
                CloseCalendar();
                _sidebarControl.SetActiveMenu("Board");
                ReloadBoard();
                return;

            case "Content":
            case "Task":
                await OpenGlobalContentAsync(result.EntityId, targetProjectId, authGeneration);
                return;

            case "Idea":
                await OpenIdeaBankPageAsync(result.EntityId);
                return;

            default:
                _toast.ShowToast(this, "Loại kết quả tìm kiếm này chưa thể mở.");
                return;
        }
    }

    private async Task OpenGlobalContentAsync(long contentId, long projectId, long authGeneration)
    {
        if (_boardBackend is null || _detailBackend is null)
        {
            _toast.ShowToast(this, "Content chưa được kết nối với API.");
            return;
        }

        long projectGeneration = _projects?.Context.Generation ?? 0;
        try
        {
            List<ContentBoardCard> cards = await _boardBackend.GetProjectContentsAsync(projectId, _authLifetime.Token);
            if (IsDisposed || !_auth!.Session.IsAuthenticated || authGeneration != _auth.Session.Generation ||
                projectGeneration != _projects?.Context.Generation || _projects?.Context.Current?.ProjectId != projectId) return;

            _allCards = cards;
            if (cards.All(card => card.ContentId != contentId))
            {
                _toast.ShowToast(this, "Content không còn tồn tại hoặc không thuộc Project này.");
                return;
            }
            OpenContentLibraryDetail(contentId);
        }
        catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ContentValidationException or UnauthorizedWorkflowActionException or
                                       InvalidOperationException or ApiException or ApiBusinessException)
        {
            if (!IsDisposed) _toast.ShowToast(this, "Không mở được Content: " + error.Message);
        }
    }
}
