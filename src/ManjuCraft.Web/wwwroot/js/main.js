$(function () {
    var currentProjectId = getCurrentProjectId();

    function getCurrentProjectId() {
        var params = new URLSearchParams(window.location.search);
        return params.get('projectId');
    }

    function setCurrentProjectId(id) {
        var url = new URL(window.location.href);
        url.searchParams.set('projectId', id);
        window.history.pushState({}, '', url);
    }

    if (currentProjectId) {
        setCurrentProjectId(currentProjectId);
    }

    $(document).on('click', '.project-card', function () {
        var id = $(this).data('id');
        setQueryParameter('projectId', id);
    });

    function setQueryParameter(key, value) {
        var url = new URL(window.location.href);
        url.searchParams.set(key, value);
        window.location.href = url.toString();
    }

    $('.sortable-list').sortable({
        handle: '.drag-handle',
        placeholder: 'sortable-placeholder',
        update: function (event, ui) {
            reorderItems($(this).closest('.sortable-list'));
        }
    }).disableSelection();

    function reorderItems($list) {
        var controller = $list.data('controller');
        var ids = [];
        $list.find('.sortable-item').each(function () {
            ids.push(parseInt($(this).data('id')));
        });
        if (ids.length === 0) return;

        $.ajax({
            url: '/api/v1/' + controller + '/reorder',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({ ids: ids }),
            success: function (res) {
                if (res.success) {
                    showToast('排序已保存');
                }
            },
            error: function () {
                showToast('排序失败', 'error');
            }
        });
    }

    function showToast(message, type) {
        var color = type === 'error' ? '#e74c3c' : '#2ecc71';
        var $toast = $('<div class="toast"></div>')
            .css('background', color)
            .text(message)
            .appendTo('body');
        setTimeout(function () {
            $toast.fadeOut(300, function () { $(this).remove(); });
        }, 2000);
    }

    $('.generate-btn').on('click', function () {
        var $btn = $(this);
        var $spinner = $btn.find('.spinner');
        $btn.prop('disabled', true);
        $spinner.show();

        var endpoint = $btn.data('endpoint');
        var entityId = $btn.data('id');
        var workflowType = $btn.data('workflow-type');

        $.ajax({
            url: '/api/v1/' + endpoint + '/' + entityId,
            type: 'POST',
            data: workflowType ? { workflowType: workflowType } : {},
            success: function (res) {
                if (res.success) {
                    showToast('任务已提交: ' + (res.data?.message || '处理中'));
                    pollTaskStatus(res.data?.taskId, entityId);
                } else {
                    showToast(res.message || '生成失败', 'error');
                    $btn.prop('disabled', false);
                    $spinner.hide();
                }
            },
            error: function () {
                showToast('请求失败', 'error');
                $btn.prop('disabled', false);
                $spinner.hide();
            }
        });
    });

    function pollTaskStatus(taskId, entityId) {
        var maxRetries = 120;
        var retries = 0;

        function check() {
            retries++;
            if (retries > maxRetries) {
                showToast('生成超时，请刷新页面查看结果', 'error');
                return;
            }
            setTimeout(function () {
                $.ajax({
                    url: '/api/v1/tasks/' + taskId + '/status',
                    success: function (res) {
                        if (res.data?.status === 'completed') {
                            showToast('生成完成！');
                            location.reload();
                        } else if (res.data?.status === 'failed') {
                            showToast('生成失败: ' + (res.data?.message || '未知错误'), 'error');
                        } else {
                            check();
                        }
                    },
                    error: function () {
                        check();
                    }
                });
            }, 5000);
        }
        check();
    }

    $('.file-upload-input').on('change', function () {
        var $input = $(this);
        var entityId = $input.data('id');
        var entityType = $input.data('entity-type');
        var viewType = $input.data('view-type');
        var file = this.files[0];

        if (!file) return;

        var formData = new FormData();
        formData.append('file', file);
        formData.append('viewType', viewType);

        $.ajax({
            url: '/api/v1/' + entityType + '/' + entityId + '/images/upload',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (res.success) {
                    showToast('上传成功，图片已更新');
                    var $img = $input.closest('.asset-card').find('.asset-image img');
                    if ($img.length) {
                        $img.attr('src', res.data?.fileUrl + '?t=' + Date.now());
                    }
                } else {
                    showToast(res.message || '上传失败', 'error');
                }
            },
            error: function () {
                showToast('上传失败', 'error');
            }
        });
    });

    $('.delete-btn').on('click', function () {
        if (!confirm('确定要删除吗？此操作不可撤销')) return;
        var endpoint = $(this).data('endpoint');
        var id = $(this).data('id');
        $.ajax({
            url: '/api/v1/' + endpoint + '/' + id,
            type: 'DELETE',
            success: function (res) {
                if (res.success) {
                    $(this).closest('.asset-card').fadeOut(300, function () { $(this).remove(); });
                    showToast('删除成功');
                }
            }.bind(this),
            error: function () {
                showToast('删除失败', 'error');
            }
        });
    });

    $('.modal-trigger').on('click', function () {
        var target = $(this).data('target');
        $(target).addClass('show active');
    });

    $('.modal-close').on('click', function () {
        $(this).closest('.modal-overlay').removeClass('show active');
    });

    $(document).on('click', '.modal-overlay', function (e) {
        if (e.target === this) {
            $(this).removeClass('show active');
        }
    });
});
