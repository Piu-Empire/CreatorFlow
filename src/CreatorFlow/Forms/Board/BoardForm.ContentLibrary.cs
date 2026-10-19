using CreatorFlow.ApiClients;
using CreatorFlow.Contracts.Board;
using CreatorFlow.Controls;
using CreatorFlow.Controls.ContentLibrary;
using CreatorFlow.Models;
using CreatorFlow.Services;
using CreatorFlow.Services.Exceptions;

namespace CreatorFlow.Forms.Board;

public partial class BoardForm
{
    /// <summary>Mở Content được chọn từ My Tasks trên Board hiện hành.</summary>
    private async void OpenMyTaskContent(long contentId)
    {
        if (!HasProjectContext || !CanLeaveProjectPage()) return;

        if (_projects is null)
        {
            CloseProjectPage();
            _sidebarControl.SetActiveMenu("Board");
            ReloadBoard();
            OpenContentById(contentId);
            return;
        }

        if (_detailBackend is null)
        {
            _toast.ShowToast(this, "Chi tiết Content chưa kết nối API.");
            return;
        }

        long projectId = CurrentSession.CurrentProjectId;
        long projectGeneration = _projects.Context.Generation;
        long authGeneration = _auth?.Session.Generation ?? 0;
        Control? sourcePage = _projectPage;
        bool IsCurrentPage() => !IsDisposed && sourcePage is not null &&
            ReferenceEquals(_projectPage, sourcePage) && HasProjectContext &&
            CurrentSession.CurrentProjectId == projectId &&
            projectGeneration == _projects.Context.Generation &&
            authGeneration == (_auth?.Session.Generation ?? 0);

        // My Tasks có thể mở trước khi Board kết thúc tải; nạp cache thư viện nếu card chưa có.
        if (_allCards.All(card => card.ContentId != contentId) && _boardBackend is not null)
        {
            try
            {
                List<ContentBoardCard> cards = await _boardBackend.GetProjectContentsAsync(projectId, _authLifetime.Token);
                if (!IsCurrentPage()) return;
                _allCards = cards;
            }
            catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested || IsDisposed) { return; }
            catch (Exception error) when (error is ContentValidationException or UnauthorizedWorkflowActionException or
                                           InvalidOperationException or ApiException or ApiBusinessException)
            {
                if (IsCurrentPage()) _toast.ShowToast(this, "Không tải được Content: " + error.Message);
                return;
            }
        }

        if (!IsCurrentPage()) return;
        OpenContentLibraryDetail(contentId);
    }

    private async Task OpenContentLibraryAsync()
    {
        if (!HasProjectContext || !CanLeaveProjectPage()) return;
        if (_projects is not null && !await ValidateProjectAsync()) return;
        if (!HasProjectContext) return;

        long projectId = CurrentSession.CurrentProjectId;
        long projectGeneration = _projects?.Context.Generation ?? 0;
        long? authGeneration = _auth?.Session.Generation;
        var page = new ContentLibraryControl(CurrentSession.CurrentProjectName)
        {
            Dock = DockStyle.Fill,
        };
        page.OpenContentRequested += OpenContentLibraryDetail;
        ShowProjectPage(page, "Kho Content");
        _sidebarControl.SetActiveMenu("Kho Content");

        bool IsCurrentPage() => !IsDisposed && !page.IsDisposed && ReferenceEquals(_projectPage, page) &&
            CurrentSession.CurrentProjectId == projectId &&
            (_projects is null || projectGeneration == _projects.Context.Generation) &&
            authGeneration == _auth?.Session.Generation;

        int queryGeneration = 0;
        async void LoadQuery(ContentLibraryQuery query)
        {
            if (_boardBackend is null) return;
            int currentGeneration = ++queryGeneration;
            try
            {
                List<ContentBoardCard> cards = await _boardBackend.GetProjectContentsAsync(projectId, query, _authLifetime.Token);
                if (!IsCurrentPage() || currentGeneration != queryGeneration) return;
                page.SetFilteredContents(cards);
            }
            catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested || IsDisposed) { }
            catch (Exception error) when (error is ContentValidationException or UnauthorizedWorkflowActionException or
                                           InvalidOperationException or ApiException or ApiBusinessException)
            {
                if (IsCurrentPage() && currentGeneration == queryGeneration)
                    _toast.ShowToast(this, "Không lọc được Content từ API: " + error.Message);
            }
        }
        page.QueryRequested += LoadQuery;

        if (_projects is null)
        {
            try
            {
                _allCards = _boardRepository.GetBoardCards(projectId);
                if (IsCurrentPage()) page.SetContents(_allCards);
            }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException)
            {
                if (IsCurrentPage()) page.SetLoadError(error.Message);
            }
            return;
        }

        if (_boardBackend is null)
        {
            page.SetLoadError("Kho Content chưa được kết nối API.");
            return;
        }

        try
        {
            List<CreatorFlow.Models.ContentBoardCard> cards = await _boardBackend.GetProjectContentsAsync(projectId, _authLifetime.Token);
            if (!IsCurrentPage()) return;
            _allCards = cards;
            page.SetContents(cards);
        }
        catch (OperationCanceledException) when (_authLifetime.IsCancellationRequested || IsDisposed) { }
        catch (Exception error) when (error is ContentValidationException or UnauthorizedWorkflowActionException or InvalidOperationException or ApiException or ApiBusinessException)
        {
            if (IsCurrentPage()) page.SetLoadError(error.Message);
        }
    }

    private void OpenContentLibraryDetail(long contentId)
    {
        if (!HasProjectContext) return;
        if (_projects is not null && _detailBackend is null)
        {
            _toast.ShowToast(this, "Chi tiết Content chưa kết nối API.");
            return;
        }

        ContentBoardCard? card = _allCards.FirstOrDefault(item => item.ContentId == contentId);
        if (card is null)
        {
            _toast.ShowToast(this, "Không tìm thấy Content trong Project hiện tại.");
            return;
        }
        if (_contentDetailPanel.HasUnsavedChanges && !_contentDetailPanel.Clear()) return;

        CloseProjectPage();
        CloseProfile();
        CloseAdmin();
        CloseReports();
        CloseCalendar();
        CloseDashboard();
        _pnlBoardArea.Visible = true;
        _modulePageHeader.Visible = true;
        _flowColumns.Visible = true;
        _pnlDrawerHost.Visible = false;
        if (_pnlBoardArea.Controls["serverProjectState"] is Label serverState) serverState.Visible = false;
        _topHeaderControl.SetAuthenticatedPage("Board");
        _sidebarControl.SetActiveMenu("Board");

        if (_projects is not null)
        {
            foreach (KanbanColumnControl column in _columns.Values) column.SourceAvailable = true;
            _modulePageHeader.SetContentCreationEnabled(CanCreateContentViaApi);
            ApplyFilters(); // Archived Content stays out of Kanban columns but remains openable from the library.
        }
        else
        {
            ReloadBoard();
            card = _allCards.FirstOrDefault(item => item.ContentId == contentId);
            if (card is null)
            {
                _toast.ShowToast(this, "Không tìm thấy Content trong Project hiện tại.");
                return;
            }
        }

        if (_selectedCardControl is not null && !_selectedCardControl.IsDisposed)
            _selectedCardControl.IsSelected = false;
        _selectedCardControl = FindCardControl(contentId);
        if (_selectedCardControl is not null) _selectedCardControl.IsSelected = true;
        _selectedContentId = contentId;
        _pnlDrawerHost.Visible = true;
        _contentDetailPanel.LoadContent(card);
    }
}
