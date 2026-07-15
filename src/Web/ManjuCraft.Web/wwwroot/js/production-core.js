// Production Core - State & Initialization
(function() {
    // Configuration - will be set from data attributes
    var projectId = 0;
    var projectName = '';

    // State
    var chapters = [];
    var currentChapterIdx = -1;
    var currentTab = 'shots';
    var shotState = {};

    // Frame templates
    var _frameTemplates = [
        { first: '晨曦微露，薄雾笼罩的村落全景', middle: '镜头缓缓扫过村庄，石板路上有早起的居民开始劳作', last: '视线逐渐拉远，远景渐入云海' },
        { first: '主角从村口大步走来，身披斗篷腰佩长剑', middle: '中景跟拍主角穿过市集，周围村民纷纷侧目低语', last: '主角在镜头前停步，缓缓转身' },
        { first: '面部大特写——主角瞳孔微颤，额头渗出细密汗珠', middle: '镜头缓慢前推至眼部特写', last: '特写维持在眉眼之间，一滴泪珠从眼角滑落' },
        { first: '过肩镜头——主角肩部占据画面右下角', middle: '正反打切换至精灵少女近景', last: '镜头回到过肩视角，精灵少女收起长弓' },
        { first: '一枚古朴的金色宝石悬浮于祭坛之上', middle: '镜头围绕宝石缓缓旋转，符文光纹逐渐变亮', last: '一只手从画面右侧缓缓伸入' },
        { first: '全景俯拍——角色在古老的森林中穿行', middle: '镜头平移跟拍角色在森林中的奔跑', last: '角色冲出树林到达悬崖边缘，豁然开朗' },
        { first: '超大远景——古战场遗迹横亘在荒原之上', middle: '镜头缓缓拉远，荒原上星罗棋布的篝火逐渐化为光点', last: '画面升向高空穿破云层' }
    ];

    // Pollers
    var _extractPoller = null;
    var _lastExtractPromptId = null;
    var _lastExtractWorkflowType = null;
    var _allProviders = [];
    var _shotAssetExtractPoller = null;
    var _extractedNewAssets = null;
    var _extractionPreviewData = null;
    var _extractionAiResponse = null;
    var _importNewAssets = null;
    var _importShotsData = null;

    // Initialize from DOM
    function initFromDOM() {
        var scriptTag = document.getElementById('production-config');
        if (scriptTag) {
            projectId = parseInt(scriptTag.dataset.projectId) || 0;
            projectName = scriptTag.dataset.projectName || '';
            window.projectId = projectId;
            window.projectName = projectName;
        }
    }

    // Main initialization
    function init() {
        initFromDOM();
        var pNameEl = document.getElementById('pName');
        if (pNameEl) pNameEl.textContent = projectName;
        loadChapters();
    }

    function loadChapters() {
        fetch('/Story/LoadChaptersForProduction?projectId=' + projectId)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (res && res.success && res.data) {
                    chapters = (res.data || []).map(function(c) {
                        if (typeof c.chapterName === 'string') {
                            c.chapterName = c.chapterName.replace(/\\u[\dA-F]{4}/gi, function(m) {
                                return String.fromCharCode(parseInt(m.replace(/\\u/, ''), 16));
                            });
                            var d = document.createElement('div');
                            d.innerHTML = c.chapterName;
                            c.chapterName = d.textContent;
                        }
                        if (typeof c.content === 'string') {
                            c.content = c.content.replace(/\\u[\dA-F]{4}/gi, function(m) {
                                return String.fromCharCode(parseInt(m.replace(/\\u/, ''), 16));
                            });
                            var d = document.createElement('div');
                            d.innerHTML = c.content;
                            c.content = d.textContent;
                        }
                        return c;
                    });
                } else {
                    chapters = [];
                }
                render();
                window.chapters = chapters;
            })
            .catch(function() { chapters = []; render(); window.chapters = chapters; });
    }

    function render() {
        renderSidebar();
        // Auto-select first chapter if none selected and chapters exist
        if (currentChapterIdx === -1 && chapters.length > 0) {
            selectChapter(0);
        } else if (currentTab === 'shots') {
            renderShotsTab();
        } else {
            renderContentTab();
        }
    }

    function renderSidebar() {
        var pStatus = document.getElementById('pStatus');
        if (pStatus) pStatus.textContent = chapters.length + ' 章节';
        var h = '';
        chapters.forEach(function(c, i) {
            var hasShots = shotState[i] && shotState[i].shots && shotState[i].shots.length > 0;
            h += '<div class="episode-item' + (i === currentChapterIdx ? ' active' : '') + '" onclick="selectChapter(' + i + ')">'
                + '<span class="episode-num">第' + (c.chapterNumber || (i+1)) + '章</span>'
                + '<div class="episode-info"><div class="episode-name">' + c.chapterName + '</div>'
                + '<div class="episode-meta">' + (hasShots ? shotState[i].shots.length + ' 分镜' : '未提取分镜') + '</div></div></div>';
        });
        var epList = document.getElementById('epList');
        if (epList) epList.innerHTML = h || '<div style="padding:20px;text-align:center;color:var(--text3);font-size:13px;">暂无章节，请先创建剧本<br><a class="back-link" style="color:var(--info);cursor:pointer;display:inline-block;margin-top:8px;" href="?type=Actor&projectId=' + projectId + '">编辑剧本</a></div>';
    }

    function selectChapter(idx) {
        currentChapterIdx = idx;
        window.currentChapterIdx = idx;
        // Set loading state BEFORE render so UI shows spinner immediately
        shotState[idx] = { shots: shotState[idx]?.shots || [], loading: true, loadingFromServer: true };
        render();
        // Force reload when user explicitly clicks a chapter
        loadShotsForChapter(idx, true).then(function() {
            render();
        });
        var mainTitle = document.getElementById('mainTitle');
        if (mainTitle) mainTitle.textContent = '第' + chapters[idx].chapterNumber + '章 — ' + chapters[idx].chapterName;
    }

    async function loadShotsForChapter(idx, forceReload) {
        // If forceReload is true, bypass cache; otherwise use cache
        if (!forceReload && shotState[idx] && shotState[idx].shots && shotState[idx].shots.length > 0) return;
        // If already loading from a previous non-forced call, skip; but allow forceReload to proceed
        if (shotState[idx] && shotState[idx].loading && !forceReload) return;
        shotState[idx] = { shots: shotState[idx]?.shots || [], loading: true, loadingFromServer: true };

        try {
            var res = await fetch('/api/v1/production/shots?projectId=' + projectId + '&chapterIdx=' + idx);
            var data = await res.json();
            if (data.success && data.data) {
                var shots = data.data.shots || [];
                var formattedShots = shots.map(function(s) {
                    return {
                        id: s.id,
                        shotNumber: s.shotNumber,
                        shotName: s.shotName,
                        shotSize: s.shotSize,
                        cameraMovement: s.cameraMovement,
                        duration: s.duration,
                        order: s.order,
                        description: s.description,
                        assetRefs: s.assetRefs,
                        assets: s.assets || [],
                        storyboardUrl: s.storyboardUrl,
                        videoUrl: s.videoUrl,
                        frames: (s.frames || []).map(function(f) {
                            return {
                                id: f.id,
                                frameType: f.frameType,
                                narrativeDescription: f.narrativeDescription || f.NarrativeDescription || '',
                                generatePrompt: f.generatePrompt || f.GeneratePrompt || '',
                                cameraMovement: f.cameraMovement || f.CameraMovement || '',
                                shotSize: f.shotSize || f.ShotSize || '',
                                description: f.narrativeDescription || f.NarrativeDescription || f.description,
                                order: f.order,
                                startTime: f.startTime,
                                duration: f.duration,
                                hasImage: !!(f.imagePath || f.resourceId),
                                imagePath: f.imagePath || f.resourceId,
                                assets: f.assets || []
                            };
                        }),
                        hasFirstFrame: false,
                        hasVideo: !!s.videoUrl
                    };
                });
                shotState[idx] = { shots: formattedShots, loading: false, loadingFromServer: false };
            } else {
                shotState[idx] = { shots: [], loading: false, loadingFromServer: false };
            }
        } catch (e) {
            console.error('Load shots failed:', e);
            shotState[idx] = { shots: [], loading: false, loadingFromServer: false };
        }
    }

    function switchTab(tab) {
        currentTab = tab;
        document.querySelectorAll('.prod-tab').forEach(function(t) { t.classList.remove('active'); if (t.dataset.tab === tab) t.classList.add('active'); });
        document.querySelectorAll('.tab-panel').forEach(function(p) { p.classList.add('hidden'); });
        var tabEl = document.getElementById('tab-' + tab);
        if (tabEl) tabEl.classList.remove('hidden');
        if (tab === 'shots') {
            if (currentChapterIdx >= 0 && (!shotState[currentChapterIdx] || !shotState[currentChapterIdx].shots || shotState[currentChapterIdx].shots.length === 0)) {
                loadShotsForChapter(currentChapterIdx, true);
            } else if (shotState[currentChapterIdx] && shotState[currentChapterIdx].loading) {
                // If currently loading, reset loading flag so render shows current state
                shotState[currentChapterIdx].loading = false;
            }
            renderShotsTab();
        } else renderContentTab();
    }

    // Expose globally
    window.init = init;
    window.loadChapters = loadChapters;
    window.render = render;
    window.renderSidebar = renderSidebar;
    window.selectChapter = selectChapter;
    window.loadShotsForChapter = loadShotsForChapter;
    window.switchTab = switchTab;
    window.chapters = chapters;
    window.currentChapterIdx = currentChapterIdx;
    window.currentTab = currentTab;
    window.shotState = shotState;
    window.projectId = projectId;
    window.projectName = projectName;

    // Clear shots for current chapter
    window.clearShots = async function() {
        if (currentChapterIdx < 0) {
            alert('请先选择章节');
            return;
        }
        if (!confirm('确定要清空当前章节的所有分镜和帧吗？此操作不可恢复。')) {
            return;
        }

        try {
            var res = await fetch('/api/v1/production/clear-shots', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ projectId: projectId, chapterIdx: currentChapterIdx })
            });
            var data = await res.json();
            if (data.success) {
                alert(data.message);
                shotState[currentChapterIdx] = { shots: [], loading: false };
                render();
            } else {
                alert('清空失败: ' + data.message);
            }
        } catch (e) {
            console.error('Clear shots failed:', e);
            alert('清空失败: ' + e.message);
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();