function setupButtonListeners() {
    document.querySelectorAll('.action-edit').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            showEditModal(this);
        });
    });
    document.querySelectorAll('.action-ai').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            var type = btn.getAttribute('data-type');
            if (type === 'Bgm' || type === 'VoiceVoice') {
                showAudioGenerateModal(this);
            } else {
                showSingleAssetGenModal(this);
            }
        });
    });
    document.querySelectorAll('.action-delete').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            var id = btn.getAttribute('data-id');
            var name = JSON.parse(btn.getAttribute('data-name'));
            deleteAssetFromId(id, name);
        });
    });
}

function showReplaceModal(btn) {
    var id = btn.getAttribute('data-id');
    var name = JSON.parse(btn.getAttribute('data-name'));
    var isBgm = btn.getAttribute('data-isbgm') === 'true';
    document.getElementById('replaceAssetId').value = id;
    document.getElementById('replaceResourceTitle').textContent = isBgm ? '替换音频: ' + name : '替换图片: ' + name;
    var fileInput = document.getElementById('replaceFileInput');
    fileInput.accept = isBgm ? "audio/*" : "image/*";
    fileInput.value = '';
    document.getElementById('replacePreviewImg').style.display = 'none';
    document.getElementById('replacePreviewImg').src = '';
    showModal('replaceResourceModal');
    setTimeout(function() { fileInput.focus(); }, 100);
}

function showAudioGenerateModal(btn) {
    var id = btn.getAttribute('data-id');
    var type = btn.getAttribute('data-type');
    var name = JSON.parse(btn.getAttribute('data-name'));
    var isBgm = type === 'Bgm';

    var card = btn.closest('.asset-detail-card');
    var desc = card ? (card.querySelector('.asset-detail-desc')?.textContent?.trim() || '') : '';

    document.getElementById('sagAssetId').value = id;
    document.getElementById('sagAssetType').value = type;
    document.getElementById('sagAssetName').value = name;
    document.getElementById('sagGeneratedImageUrl').value = '';

    var typeName = isBgm ? 'BGM' : '角色声音';
    document.getElementById('singleAssetGenTitle').textContent = '单个资产 AI 生成音频: ' + name + ' (' + typeName + ')';

    loadAssetGenTemplates().then(function(templates) {
        var templateKeyMap = {
            'Bgm': 'BGM生成提示词',
            'VoiceVoice': '角色声音生成提示词'
        };
        var templateContent = templates[templateKeyMap[type]] || '';

        var prompts = {
            'Bgm': '示例：轻快愉悦的钢琴曲，适合日常生活场景，节目场景BGM，循环播放',
            'VoiceVoice': '示例：温柔甜美的少女音，语速适中，带有治愈感，适合旁白或对话'
        };
        var promptEl = document.getElementById('sagPrompt');
        promptEl.placeholder = prompts[type] || '输入描述词...';
        document.getElementById('sagPromptHint').textContent = prompts[type] || '';

        if (desc) {
            promptEl.value = desc + '\n';
        } else {
            promptEl.value = '';
        }

        var templateDisplay = document.getElementById('sagTemplatePromptDisplay');
        if (templateContent) {
            templateDisplay.value = templateContent;
            document.getElementById('sagTemplateGroup').style.display = '';
        } else {
            document.getElementById('sagTemplateGroup').style.display = 'none';
        }
    });

    document.getElementById('sagPreviewGroup').style.display = 'none';
    document.getElementById('sagPreviewImg').style.display = 'none';
    document.getElementById('sagPreviewPlaceholder').style.display = '';
    document.getElementById('sagGenerateBtn').style.display = '';
    document.getElementById('sagSaveBtn').style.display = 'none';
    document.getElementById('sagCloseBtn').textContent = '关闭';
    document.getElementById('sagStatus').style.display = 'none';
    document.getElementById('sagStatus').innerHTML = '';

    loadSingleAssetGenModels(type);

    showModal('singleAssetGenModal');
}

function showSingleAssetGenModal(btn) {
    var id = btn.getAttribute('data-id');
    var type = btn.getAttribute('data-type');
    var name = JSON.parse(btn.getAttribute('data-name'));

    var card = btn.closest('.asset-detail-card');
    var desc = card ? (card.querySelector('.asset-detail-desc')?.textContent?.trim() || '') : '';

    document.getElementById('sagAssetId').value = id;
    document.getElementById('sagAssetType').value = type;
    document.getElementById('sagAssetName').value = name;
    document.getElementById('sagGeneratedImageUrl').value = '';

    var typeNames = { 'Actor': '角色', 'Scene': '场景', 'Prop': '道具' };
    var typeName = typeNames[type] || type;
    document.getElementById('singleAssetGenTitle').textContent = '单个资产 AI 生成图片: ' + name + ' (' + typeName + ')';

    loadAssetGenTemplates().then(function(templates) {
        var templateKeyMap = {
            'Actor': '角色档案生成提示词',
            'Scene': '场景档案生成提示词',
            'Prop': '道具档案生成提示词'
        };
        var templateContent = templates[templateKeyMap[type]] || '';

        var prompts = {
            'Actor': '示例：穿着红色长裙的女性角色，正面视角，白背景，动漫风格，高质量',
            'Scene': '示例：古代京城街道，青石板路，两侧店铺林立，晨光穿过屋檐，赛博朋克风格',
            'Prop': '示例：精致的青瓷碗，热气腾腾，白瓷质感，特写镜头，产品摄影风格'
        };
        var promptEl = document.getElementById('sagPrompt');
        promptEl.placeholder = prompts[type] || '输入描述词...';
        document.getElementById('sagPromptHint').textContent = prompts[type] || '';

        if (desc) {
            promptEl.value = desc + '\n';
        } else {
            promptEl.value = '';
        }

        var templateDisplay = document.getElementById('sagTemplatePromptDisplay');
        if (templateContent) {
            templateDisplay.value = templateContent;
            document.getElementById('sagTemplateGroup').style.display = '';
        } else {
            document.getElementById('sagTemplateGroup').style.display = 'none';
        }
    });

    document.getElementById('sagPreviewGroup').style.display = 'none';
    document.getElementById('sagPreviewImg').style.display = 'none';
    document.getElementById('sagPreviewPlaceholder').style.display = '';
    document.getElementById('sagGenerateBtn').style.display = '';
    document.getElementById('sagSaveBtn').style.display = 'none';
    document.getElementById('sagCloseBtn').textContent = '关闭';
    document.getElementById('sagStatus').style.display = 'none';
    document.getElementById('sagStatus').innerHTML = '';

    loadSingleAssetGenModels(type);

    showModal('singleAssetGenModal');
}

function loadAssetGenTemplates() {
    if (_assetGenTemplates) return Promise.resolve(_assetGenTemplates);
    return apiGetGenerationTemplates()
        .then(function(templates) {
            _assetGenTemplates = templates;
            return templates;
        })
        .catch(function() {
            _assetGenTemplates = {};
            return {};
        });
}

var _assetGenTemplates = null;

async function loadSingleAssetGenModels(assetType) {
    var select = document.getElementById('sagModelSelect');
    select.innerHTML = '<option value="">加载中...</option>';
    select.disabled = true;

    try {
        var imageProviders = await apiGetImageModels();

        if (imageProviders.length === 0) {
            select.innerHTML = '<option value="">暂无可用的文生图模型，请先配置 ComfyUI</option>';
            select.disabled = true;
            return;
        }

        _sagModels = imageProviders;
        select.innerHTML = '';
        imageProviders.forEach(function(p, idx) {
            var opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.name + ' (' + (p.model || 'default') + ')';
            if (idx === 0) opt.selected = true;
            select.appendChild(opt);
        });
        select.disabled = false;
    } catch (err) {
        select.innerHTML = '<option value="">加载失败: ' + err.message + '</option>';
        select.disabled = true;
    }
}

var _sagModels = [];

async function doSingleAssetGenerate() {
    var assetId = document.getElementById('sagAssetId').value;
    var assetType = document.getElementById('sagAssetType').value;
    var modelId = document.getElementById('sagModelSelect').value;
    var prompt = document.getElementById('sagPrompt').value.trim();
    var templateContent = document.getElementById('sagTemplatePromptDisplay').value.trim();

    if (!modelId) { alert('请选择生成模型'); return; }
    if (!prompt) { alert('请输入提示词'); return; }

    var genBtn = document.getElementById('sagGenerateBtn');
    var saveBtn = document.getElementById('sagSaveBtn');
    var closeBtn = document.getElementById('sagCloseBtn');
    var statusEl = document.getElementById('sagStatus');
    var previewGroup = document.getElementById('sagPreviewGroup');
    var previewImg = document.getElementById('sagPreviewImg');
    var previewPlaceholder = document.getElementById('sagPreviewPlaceholder');

    genBtn.disabled = true;
    genBtn.textContent = '生成中...';
    closeBtn.disabled = true;
    statusEl.style.display = '';
    statusEl.className = 'status-info';
    statusEl.innerHTML = '🔄 正在提交生成任务...';

    try {
        var model = _sagModels.find(function(m) { return m.id == modelId; });
        if (!model) throw new Error('未找到选中的模型');

        var fullPrompt = templateContent + '\n\n' + prompt;

        var endpoint = '';
        var workflowType = '';
        var isAudio = assetType === 'Bgm' || assetType === 'VoiceVoice';

        if (isAudio) {
            if (assetType === 'Bgm') {
                endpoint = '/api/comfyui/stable-bgm/generate';
                workflowType = 'stable-bgm-generate';
            } else {
                endpoint = '/api/comfyui/ace-music/compose';
                workflowType = 'ace-music-compose';
            }
        } else {
            endpoint = '/Assets/GenerateCharacterImage';
            workflowType = 'zimage-character-profile';
        }

        var payload;
        if (isAudio) {
            payload = { prompt: fullPrompt };
        } else {
            payload = {
                systemPrompt: templateContent,
                characterPrompt: prompt,
                negativePrompt: '',
                width: 1024,
                height: 768
            };
        }

        var res = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        var data = await res.json();

        var promptId;
        if (isAudio) {
            promptId = data && data.promptId;
            if (!promptId) {
                throw new Error(data.message || '提交任务失败');
            }
        } else {
            if (!data.success) {
                throw new Error(data.message || '提交任务失败');
            }
            promptId = data.promptId;
        }

        statusEl.innerHTML = '⏳ 任务已提交，正在生成中... (promptId: ' + promptId + ')';

        var resultUrl = await pollSingleAssetGenResult(promptId, workflowType, isAudio);
        if (!resultUrl) throw new Error('生成超时或失败');

        if (isAudio) {
            statusEl.className = 'status-success';
            statusEl.innerHTML = '✅ 音频生成成功！<br><audio controls src="' + resultUrl + '" style="width:100%;margin-top:8px;"></audio>';
            document.getElementById('sagGeneratedImageUrl').value = resultUrl;
        } else {
            previewGroup.style.display = '';
            previewImg.src = resultUrl;
            previewImg.style.display = '';
            previewPlaceholder.style.display = 'none';
            document.getElementById('sagGeneratedImageUrl').value = resultUrl;

            statusEl.className = 'status-success';
            statusEl.innerHTML = '✅ 图片生成成功！';
        }

        genBtn.style.display = 'none';
        saveBtn.style.display = '';
        closeBtn.textContent = '取消';
        closeBtn.disabled = false;

    } catch (err) {
        statusEl.className = 'status-error';
        statusEl.innerHTML = '❌ 生成失败: ' + err.message;
        genBtn.disabled = false;
        genBtn.textContent = '重新生成';
        closeBtn.disabled = false;
    }
}

function pollSingleAssetGenResult(promptId, workflowType, isAudio) {
    return new Promise(function(resolve, reject) {
        var pollCount = 0;
        var maxPolls = 120;
        var pollInterval = 5000;

        var timer = setInterval(async function() {
            pollCount++;
            if (pollCount >= maxPolls) {
                clearInterval(timer);
                reject(new Error('生成超时'));
                return;
            }

            try {
                var resultType = isAudio ? 'audio' : 'image';
                var url = '/api/v1/comfyui/result/' + promptId + '/' + resultType + '?workflowType=' + encodeURIComponent(workflowType);
                var res = await fetch(url);
                var data = await res.json();

                if (data.success && data.data) {
                    var urls = [];
                    var d = data.data;
                    if (isAudio) {
                        if (d.audioUrls && Array.isArray(d.audioUrls)) urls = d.audioUrls;
                    } else {
                        if (d.imageUrls && Array.isArray(d.imageUrls)) urls = d.imageUrls;
                    }
                    
                    if (urls.length > 0) {
                        clearInterval(timer);
                        resolve(urls[0]);
                        return;
                    }
                } else if (data.pending === false) {
                    clearInterval(timer);
                    reject(new Error(data.message || '任务执行失败'));
                    return;
                }
            } catch (e) {}
        }, pollInterval);
    });
}

async function saveSingleAssetImage() {
    var assetId = document.getElementById('sagAssetId').value;
    var assetType = document.getElementById('sagAssetType').value;
    var resourceUrl = document.getElementById('sagGeneratedImageUrl').value;
    var isAudio = assetType === 'Bgm' || assetType === 'VoiceVoice';
    var saveBtn = document.getElementById('sagSaveBtn');
    var closeBtn = document.getElementById('sagCloseBtn');
    var statusEl = document.getElementById('sagStatus');

    if (!resourceUrl) { alert('没有可保存的资源'); return; }

    saveBtn.disabled = true;
    saveBtn.textContent = '保存中...';
    closeBtn.disabled = true;
    statusEl.className = 'status-info';
    statusEl.innerHTML = '🔄 正在保存到资产...';

    try {
        var res = await apiSaveAssetResource(assetId, isAudio, resourceUrl);

        if (res.success) {
            statusEl.className = 'status-success';
            statusEl.innerHTML = '✅ ' + (isAudio ? '音频' : '图片') + '已保存到资产！';
            showToast('资源已更新', 'success');
            setTimeout(function() {
                hideModal('singleAssetGenModal');
                window.location.reload();
            }, 1000);
        } else {
            throw new Error(res.message || '保存失败');
        }
    } catch (err) {
        statusEl.className = 'status-error';
        statusEl.innerHTML = '❌ 保存失败: ' + err.message;
        saveBtn.disabled = false;
        saveBtn.textContent = '保存到资产';
        closeBtn.disabled = false;
    }
}