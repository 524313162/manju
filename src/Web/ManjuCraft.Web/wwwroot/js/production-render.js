// Production Render - Shot rendering functions (compact, video-friendly layout)
(function() {
    // Render shots tab
    function renderShotsTab() {
        var area = document.getElementById('storyboardArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = '<div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">'
                + '<div class="icon" style="font-size:40px;margin-bottom:12px;">&#128214;</div>'
                + '<p>请先编辑剧本添加章节</p><p style="font-size:12px;color:var(--text3);margin-top:8px;"><a href="/Story?projectId=' + window.projectId + '" style="color:var(--info);text-decoration:none;">&#128214; 去编辑剧本</a></p></div>';
            return;
        }

        var ch = window.chapters[window.currentChapterIdx];
        if (!window.shotState[window.currentChapterIdx]) window.shotState[window.currentChapterIdx] = { shots: [], loading: false };

        var state = window.shotState[window.currentChapterIdx];
        var h = '<div style="margin-bottom:12px;"><strong>' + ch.chapterName + '</strong></div>';

        if (state.loading) {
            var loadingMsg = state.loadingFromServer ? '正在加载分镜数据...' : '正在调用 AI 生成分镜...';
            h += '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div>'
                + '<p style="margin-top:10px;color:var(--text2);font-size:13px;">' + loadingMsg + '</p></div>';
        } else if (state.shots && state.shots.length > 0) {
            h += '<div style="display:flex;gap:8px;margin-bottom:12px;flex-wrap:wrap;">'
                + '<button class="btn btn-primary btn-sm" onclick="regenerateShots()">&#129302; 重新提取分镜</button>'
                + '<button class="btn btn-ghost btn-sm" onclick="showFrameTemplates()">&#129300; 分帧模板</button>'
                + '</div>';

            h += '<div style="display:flex;flex-direction:column;gap:10px;">';
            state.shots.forEach(function(shot, si) {
                var frames = shot.frames || [];
                var firstFrame = frames[0];
                var otherFrames = frames.slice(1);
                var shotDuration = shot.duration || 0;
                var totalFrameDuration = frames.reduce(function(sum, f) { return sum + (f.duration || 0); }, 0);
                var shotHasVideo = shot.hasVideo && shot.videoUrl;
                var isGeneratingVideo = shot.generatingVideo;

                // Video width at 250px height with 16:9 ratio = 250 * 16/9 ≈ 444px
                var videoWidth = 444;

                h += '<div class="shot-item" style="background:var(--surface);border:1px solid var(--border);border-radius:10px;overflow:hidden;">'
                    // Shot header
                    + '<div style="display:flex;justify-content:space-between;align-items:center;padding:10px 12px;background:var(--bg);border-bottom:1px solid var(--border);flex-wrap:wrap;gap:8px;">'
                    + '<div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">'
                    + '<span class="shot-badge" style="font-size:11px;font-weight:600;padding:2px 8px;background:var(--primary);color:white;border-radius:4px;">SHOT ' + (si+1) + '</span>'
                    + (shot.shotName ? '<span style="font-size:13px;font-weight:500;color:var(--text);">' + shot.shotName + '</span>' : '')
                    + '</div>'
                    + '<div style="display:flex;align-items:center;gap:10px;font-size:11px;color:var(--text3);">'
                    + (shot.shotSize ? '<span>📐 ' + shot.shotSize + '</span>' : '')
                    + (shot.cameraMovement ? '<span>🎥 ' + shot.cameraMovement + '</span>' : '')
                    + '<span>⏱️ ' + shotDuration + 's</span>'
                    + (totalFrameDuration > 0 ? '<span>帧累计: ' + totalFrameDuration.toFixed(1) + 's</span>' : '')
                    + (shotHasVideo ? '<span style="color:var(--ok);font-weight:500;">▶ 视频就绪</span>' : (isGeneratingVideo ? '<span style="color:var(--info);font-weight:500;"><span class="spinner-inline"></span> 生成中...</span>' : ''))
                    + '</div></div>'

                    // Action bar
                    + '<div style="display:flex;gap:6px;padding:8px 12px;flex-wrap:wrap;align-items:center;">'
                    + (shotHasVideo || isGeneratingVideo
                        ? '<button class="btn btn-primary btn-xs" onclick="playShotVideo(' + si + ')" style="font-size:11px;">▶ 打开视频</button>'
                        : '<button class="btn btn-primary btn-xs" onclick="generateShotVideo(' + si + ')" ' + (isGeneratingVideo ? 'disabled' : '') + ' style="font-size:11px;">🎬 生成视频</button>')
                    + '<button class="btn btn-ghost btn-xs" onclick="generateFrameImage(' + si + ', 0)" style="font-size:11px;">🖼️ 生成首帧</button>'
                    + '<button class="btn btn-ghost btn-xs" onclick="showShotAssetBindModal(' + si + ')" style="font-size:11px;">🏷️ 绑定资产</button>'
                    + '</div>'

                    // Generation status (inline in chain)
                    + (shot.generatingFrame
                        ? '<div style="padding:6px 12px;background:var(--bg);color:var(--text2);font-size:11px;"><span class="spinner-inline"></span> 生成帧中...</div>'
                        : '')

                    // LAYOUT: Row 1 = two columns (Frames left + Video right), Row 2 = Assets
                    + '<div style="padding:12px;">'
                        // Row 1: Two columns - Frames (left, flex:1) + Video (right, fixed width)
                        + '<div style="display:flex;gap:12px;height:250px;">'
                            // LEFT: Frames horizontal scroll - takes remaining width, height 250px
                            + '<div style="flex:1;min-width:0;overflow-x:auto;padding:4px 0;height:250px;display:flex;flex-direction:row;">'
                            + window.renderFrameCard(firstFrame, si, 0, true, shot.assets)
                            + otherFrames.map(function(f, fi) {
                                var actualIdx = fi + 1;
                                return window.renderFrameCard(f, si, actualIdx, false, shot.assets);
                            }).join('')
                            + '</div>'

                            // RIGHT: Video - fixed width (16:9 at 250px = 444px), height 250px
                            + '<div style="flex:0 0 ' + videoWidth + 'px;max-width:' + videoWidth + 'px;display:flex;flex-direction:column;height:250px;">'
                            + renderVideoArea(shot, si, shotHasVideo, isGeneratingVideo)
                            + '</div>'
                        + '</div>'

                        // Row 2: Assets horizontal scroll
                        + '<div style="display:flex;gap:8px;overflow-x:auto;padding:8px 0 0;">'
                        + renderAssetsRow(shot.assets)
                        + '</div>'
                    + '</div>'
                    + '</div>';  // Close shot-item
            });
            h += '</div>';  // Close shots container
        } else {
            h += '<div style="text-align:center;padding:40px 20px;color:var(--text3);">'
                + '<div class="icon" style="font-size:40px;margin-bottom:12px;">&#127916;</div>'
                + '<p>该章节暂无分镜数据</p><p style="font-size:12px;color:var(--text3);margin-top:8px;">'
                + '<button class="btn btn-primary btn-sm" onclick="showShotAssetExtractionModal()">&#129302; 提取分镜和资产</button></p></div>';
        }

        area.innerHTML = h;
    }

// Assets row - horizontal scroll (Row 2)
    function renderAssetsRow(shotAssets) {
        if (!shotAssets || shotAssets.length === 0) {
            return '<div style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text3);padding:20px;min-width:200px;">'
                + '<span style="font-size:24px;">📦</span>'
                + '<div style="font-size:12px;margin-left:8px;">暂无绑定资产</div>'
                + '</div>';
        }

        var typeIcons = { 'Actor': '👤', 'Scene': '🏞️', 'Bgm': '🎵', 'Prop': '📦', 'VoiceVoice': '🎤' };
        var typeNames = { 'Actor': '角色', 'Scene': '场景', 'Bgm': 'BGM', 'Prop': '道具', 'VoiceVoice': '音色' };

        return shotAssets.map(function(asset) {
            var icon = typeIcons[asset.assetType] || '📄';
            var typeName = typeNames[asset.assetType] || asset.assetType || '未知';
            var role = asset.role ? '<span style="background:var(--primary);color:white;padding:1px 6px;border-radius:3px;font-size:9px;margin-left:4px;">' + asset.role + '</span>' : '';
            var imgSrc = asset.resourceFilePath || asset.ResourceFilePath || '';
            var imgHtml = imgSrc
                ? '<img src="' + imgSrc + '" style="width:100%;height:80px;object-fit:cover;border-radius:4px;" alt="' + (asset.name || asset.Name || '') + '" onclick="showImagePreview(\'' + imgSrc + '\')" title="点击放大">'
                : '<div style="width:100%;height:80px;background:var(--surface2);border:1px dashed var(--border);border-radius:4px;display:flex;align-items:center;justify-content:center;color:var(--text3);font-size:11px;">无图片</div>';

            return '<div style="flex:0 0 220px;min-width:220px;background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:8px;">'
                + '<div style="display:flex;align-items:center;gap:6px;margin-bottom:4px;">'
                + '<span style="font-size:16px;">' + icon + '</span>'
                + '<span style="font-weight:600;font-size:12px;flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' + (asset.name || asset.Name || '') + '</span>'
                + role
                + '<span style="color:var(--text3);font-size:10px;">(' + typeName + ')</span>'
                + '</div>'
                + imgHtml
                + '</div>';
        }).join(' ');
    }

    // Video area rendering - 16:9 aspect ratio, fills container
    function renderVideoArea(shot, shotIdx, hasVideo, isGenerating) {
        return '<div style="flex:1;display:flex;flex-direction:column;min-width:0;">'
            // Header
            + '<div style="display:flex;align-items:center;justify-content:space-between;padding:8px 12px;background:var(--bg);border-bottom:1px solid var(--border);flex-shrink:0;">'
            + '<span style="display:flex;align-items:center;gap:6px;font-weight:500;font-size:13px;">'
            + '<span>🎬</span>'
            + '<span>视频预览</span>'
            + (hasVideo ? '<span style="color:var(--ok);font-size:11px;">✓ 已生成</span>' : (isGenerating ? '<span style="color:var(--info);font-size:11px;"><span class="spinner-inline"></span> 生成中...</span>' : '<span style="color:var(--text3);font-size:11px;">未生成</span>'))
            + '</span>'
            + '</div>'
            // Video content - 16:9 aspect ratio via aspect-ratio CSS, limited max-height
            + '<div style="position:relative;background:#000;border-radius:0 0 8px 8px;overflow:hidden;aspect-ratio:16/9;max-height:250px;width:100%;">'
            + (hasVideo && shot.videoUrl
                ? '<video src="' + shot.videoUrl + '" controls style="width:100%;height:100%;object-fit:contain;background:#000;"></video>'
                    + '<div style="position:absolute;top:8px;right:8px;display:flex;gap:6px;">'
                    + '<button class="btn btn-ghost btn-xs" onclick="showImagePreview(\'' + shot.videoUrl + '\')" style="background:rgba(0,0,0,0.7);color:#fff;border:none;">🔍 全屏</button>'
                    + '<button class="btn btn-ghost btn-xs" onclick="regenerateShotVideo(' + shotIdx + ')" style="background:rgba(0,0,0,0.7);color:#fff;border:none;">🔄 重新生成</button>'
                    + '</div>'
                : isGenerating
                    ? '<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);">'
                        + '<span class="spinner" style="width:32px;height:32px;"></span>'
                        + '<div style="font-size:13px;font-weight:500;">视频生成中...</div>'
                        + '<div style="font-size:11px;color:var(--text3);">请耐心等待，通常需要 1-3 分钟</div>'
                        + '</div>'
                    : '<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);cursor:pointer;" onclick="generateShotVideo(' + shotIdx + ')" title="点击生成视频">'
                        + '<span style="font-size:40px;">🎬</span>'
                        + '<div style="font-size:14px;font-weight:600;">点击生成视频</div>'
                        + '<div style="font-size:11px;color:var(--text3);">基于所有帧描述生成分镜视频</div>'
                        + '</div>')
            + '</div>';
}

// Frame card - all frames use compact size in horizontal scroll
    function renderFrameCard(frame, shotIdx, frameIdx, isFirstFrame, shotAssets) {
        if (!frame) return '';

        var frameType = frame.frameType || (frameIdx === 0 ? 'First' : frameIdx === (frame.totalFrames - 1 || 99) ? 'Last' : 'Middle');
        var typeLabel = frameType === 'First' ? '首帧' : frameType === 'Last' ? '末帧' : '中帧';
        var typeColor = frameType === 'First' ? 'var(--info)' : frameType === 'Last' ? 'var(--danger)' : 'var(--ok)';
        var hasImage = frame.hasImage && frame.imagePath;
        var isGenerating = frame.generating;

        // Frame card fills 250px container height; image ~180px, info ~70px
        var cardHeight = '250px';
        var imgHeight = '180px';
        var fontSize = '12px';
        var badgeSize = '9px';

        return '<div class="frame-card" style="background:var(--bg);border:1px solid var(--border);border-radius:8px;overflow:hidden;flex:0 0 200px;min-width:200px;max-width:220px;height:' + cardHeight + ';display:flex;flex-direction:column;">'
            // Image area - click to generate
            + '<div style="width:100%;height:' + imgHeight + ';position:relative;background:var(--surface2);overflow:hidden;">'
            + (hasImage
                ? '<img src="' + frame.imagePath + '" style="width:100%;height:100%;object-fit:cover;cursor:zoom-in;" onclick="showImagePreview(\'' + frame.imagePath + '\')" title="点击放大">'
                : '<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--text3);cursor:pointer;" onclick="generateFrameImage(' + shotIdx + ', ' + frameIdx + ')" title="点击生成此帧">'
                    + '<span style="font-size:36px;">&#128247;</span>'
                    + '<span style="font-size:13px;font-weight:500;">点击生成</span>'
                    + '</div>')
            + (isGenerating ? '<div style="position:absolute;inset:0;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;color:white;font-size:12px;"><span class="spinner-inline" style="margin-right:6px;"></span>生成中...</div>' : '')
            + '<div style="position:absolute;top:4px;left:4px;background:' + typeColor + ';color:white;padding:1px 6px;border-radius:3px;font-size:9px;font-weight:600;">' + typeLabel + '</div>'
            + '</div>'

            // Info area - fills remaining height
            + '<div style="flex:1;padding:8px;display:flex;flex-direction:column;overflow:hidden;">'
            + '<div style="font-size:' + fontSize + ';color:var(--text);font-weight:500;line-height:1.4;margin-bottom:6px;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;">' + (frame.description || '暂无描述') + '</div>'
            + '<div style="display:flex;flex-wrap:wrap;gap:6px;font-size:' + badgeSize + ';color:var(--text3);margin-top:auto;">'
            + (frame.startTime !== undefined && frame.startTime !== null ? '<span>🕐 ' + frame.startTime + 's</span>' : '')
            + (frame.duration ? '<span>⏱️ ' + frame.duration + 's</span>' : '')
            + '<span>#' + (frame.order ?? frameIdx) + '</span>'
            + '</div>'

            // Assets - LARGER badges
            + (shotAssets && shotAssets.length > 0
                ? '<div style="margin-top:8px;padding-top:6px;border-top:1px solid var(--border);">'
                    + shotAssets.slice(0, 4).map(function(asset) {
                        var typeIcon = asset.assetType === 'Actor' ? '👤' : asset.assetType === 'Scene' ? '🏞️' : asset.assetType === 'Bgm' ? '🎵' : asset.assetType === 'Prop' ? '📦' : asset.assetType === 'VoiceVoice' ? '🎤' : '📄';
                        var typeName = asset.assetType || '未知';
                        return '<span style="display:inline-flex;align-items:center;gap:4px;background:var(--surface2);padding:3px 8px;border-radius:4px;font-size:11px;white-space:nowrap;">'
                            + '<span>' + typeIcon + '</span><span style="font-weight:500;">' + (asset.name || asset.Name || '') + '</span>'
                            + '<span style="color:var(--text3);font-size:10px;">(' + typeName + ')</span></span>';
                    }).join(' ')
                    + (shotAssets.length > 4 ? '<span style="font-size:11px;color:var(--text3);">+ ' + (shotAssets.length - 4) + ' 更多</span>' : '')
                    + '</div>'
                : '')

            + '</div></div>';
    }

    // Video player - refresh inline video area
    function playShotVideo(shotIdx) {
        var state = window.shotState[window.currentChapterIdx];
        if (!state || !state.shots[shotIdx]) return;
        var shot = state.shots[shotIdx];
        var area = document.getElementById('videoArea_' + shotIdx);
        if (!area) return;

        area.innerHTML = renderVideoArea(shot, shotIdx, shot.hasVideo && !!shot.videoUrl, shot.generatingVideo);
    }

    function regenerateShotVideo(shotIdx) {
        generateShotVideo(shotIdx);
    }

    function renderContentTab() {
        var area = document.getElementById('contentArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = '<div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">'
                + '<div class="icon" style="font-size:40px;margin-bottom:12px;">&#128214;</div>'
                + '<p>暂无章节内容</p></div>';
            return;
        }

        var ch = window.chapters[window.currentChapterIdx];
        var h = '<div style="background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:24px;">'
            + '<h3 style="font-size:18px;font-weight:700;margin-bottom:4px;">' + ch.chapterNumber + ' — ' + ch.chapterName + '</h3>'
            + '<div class="content-body" style="margin-top:16px;">' + formatMarkdown(ch.content || '') + '</div></div>';
        area.innerHTML = h;
    }

    // Expose globally
    window.renderShotsTab = renderShotsTab;
    window.renderFrameCard = renderFrameCard;
    window.playShotVideo = playShotVideo;
    window.regenerateShotVideo = regenerateShotVideo;
    window.renderContentTab = renderContentTab;
})();