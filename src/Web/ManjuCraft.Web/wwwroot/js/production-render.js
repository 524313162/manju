// Production Render - Shot rendering functions (compact, video-friendly layout)
(function() {

// ============ 核心渲染：Shots Tab ============
    function renderShotsTab() {
        const area = document.getElementById('storyboardArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = `
                <div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">📖</div>
                    <p>请先编辑剧本添加章节</p>
                    <p style="font-size:12px;color:var(--text3);margin-top:8px;">
                        <a href="/Story?projectId=${window.projectId}" style="color:var(--info);text-decoration:none;">📖 去编辑剧本</a>
                    </p>
                </div>`;
            return;
        }

        const ch = window.chapters[window.currentChapterIdx];
        if (!window.shotState[window.currentChapterIdx]) window.shotState[window.currentChapterIdx] = { shots: [], loading: false };

        const state = window.shotState[window.currentChapterIdx];

        if (state.loading) {
            const msg = state.loadingFromServer ? '正在加载分镜数据...' : '正在调用 AI 生成分镜...';
            area.innerHTML = `
                <div style="text-align:center;padding:20px;">
                    <div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div>
                    <p style="margin-top:10px;color:var(--text2);font-size:13px;">${msg}</p>
                </div>`;
            return;
        }

        if (state.shots && state.shots.length > 0) {
            const videoWidth = 444; // 250 * 16/9

            let h = `
                <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px;gap:12px;flex-wrap:wrap;">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <strong>${ch.chapterName}</strong>
                    </div>
                    <div style="display:flex;gap:8px;">
                        <button class="btn btn-primary btn-sm" onclick="regenerateShots()">🔄 重新提取分镜</button>
                        <button class="btn btn-ghost btn-sm" onclick="showFrameTemplates()">📋 分帧模板</button>
                    </div>
                </div>`;

            h += '<div style="display:flex;flex-direction:column;gap:10px;">';

            state.shots.forEach((shot, si) => {
                const frames = shot.frames || [];
                const shotDuration = shot.duration || 0;
                const totalFrameDuration = frames.reduce((sum, f) => sum + (f.duration || 0), 0);
                const shotHasVideo = shot.hasVideo && shot.videoUrl;
                const isGeneratingVideo = shot.generatingVideo;

                h += `<div class="shot-item" id="shot-${si}" style="background:var(--surface);border:1px solid var(--border);border-radius:10px;overflow:hidden;">`;

                // Header
                h += `
                    <div style="display:flex;justify-content:space-between;align-items:center;padding:10px 12px;background:var(--bg);border-bottom:1px solid var(--border);flex-wrap:wrap;gap:8px;">
                        <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">
                            <span class="shot-badge" style="font-size:11px;font-weight:600;padding:2px 8px;background:var(--primary);color:white;border-radius:4px;">SHOT ${si+1}</span>
                            ${shot.shotName ? `<span style="font-size:13px;font-weight:500;color:var(--text);">${shot.shotName}</span>` : ''}
                        </div>
                        <div style="display:flex;align-items:center;gap:10px;font-size:11px;color:var(--text3);">
                            ${shot.shotSize ? `<span>📐 ${shot.shotSize}</span>` : ''}
                            ${shot.cameraMovement ? `<span>🎥 ${shot.cameraMovement}</span>` : ''}
                            <span>⏱️ ${shotDuration}s</span>
                            ${totalFrameDuration > 0 ? `<span>帧累计: ${totalFrameDuration.toFixed(1)}s</span>` : ''}
                            ${shotHasVideo ? '<span style="color:var(--ok);font-weight:500;">▶ 视频就绪</span>' : (isGeneratingVideo ? '<span style="color:var(--info);font-weight:500;"><span class="spinner-inline"></span> 生成中...</span>' : '')}
                        </div>
                        <button class="shot-toggle" onclick="toggleShotFrames(${si})" style="width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:12px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;flex-shrink:0;" title="展开/收起帧" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>
                    </div>`;

                // Collapsible content wrapper (includes action bar, description, frames, divider, assets)
                h += `
                    <div id="shot-content-${si}">`;

                // Shot Description (if exists) - inside collapsible wrapper
                if (shot.description) {
                    const shotDesc = shot.description;
                    const isLongShotDesc = shotDesc.length > 200;
                    h += `
                        <div style="padding:8px 12px;background:var(--surface);border-bottom:1px solid var(--border);">
                            <div class="shot-desc" data-full="${shotDesc.replace(/"/g, '"')}" data-collapsed="${isLongShotDesc ? 'true' : 'false'}" style="font-size:12px;color:var(--text2);line-height:1.6;${isLongShotDesc ? 'display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;' : ''}">${shotDesc}</div>
                            ${isLongShotDesc ? `<button class="shot-desc-toggle" onclick="toggleShotDesc('shot-${si}')" style="margin-top:4px;width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:11px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;" title="展开/收起" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>` : ''}
                        </div>`;
                }

                // Action bar
                h += `
                        <div style="display:flex;gap:6px;padding:8px 12px;flex-wrap:wrap;align-items:center;">
                            ${shotHasVideo || isGeneratingVideo
                                ? `<button class="btn btn-primary btn-xs" onclick="playShotVideo(${si})" style="font-size:11px;">▶ 打开视频</button>`
                                : `<button class="btn btn-primary btn-xs" onclick="generateShotVideo(${si})" ${isGeneratingVideo ? 'disabled' : ''} style="font-size:11px;">🎬 生成视频</button>`
                            }
                            <button class="btn btn-ghost btn-xs" onclick="generateFrameImage(${si}, 0)" style="font-size:11px;">🖼️ 生成首帧</button>
                            <button class="btn btn-ghost btn-xs" onclick="showShotAssetBindModal(${si})" style="font-size:11px;">🏷️ 绑定资产</button>
                        </div>

                        <!-- Generation status -->
                        ${shot.generatingFrame ? `<div style="padding:6px 12px;background:var(--bg);color:var(--text2);font-size:11px;"><span class="spinner-inline"></span> 生成帧中...</div>` : ''}

                        <!-- Frames + Video area -->
                        <div id="shot-frames-${si}" style="display:flex;gap:12px;align-items:flex-start;padding:12px;">
                            <!-- LEFT: Frames vertical scroll -->
                            <div style="flex:1;min-width:0;max-height:200px;overflow-y:auto;padding:4px 0;display:flex;flex-direction:column;gap:8px;">
                                ${frames.map((f, fi) => renderFrameCard(f, si, fi, fi === 0, shot.assets)).join('')}
                            </div>

                            <!-- RIGHT: Video fixed width 444px (16:9 at 250px height) -->
                            <div style="flex:0 0 ${videoWidth}px;max-width:${videoWidth}px;display:flex;flex-direction:column;">
                                ${renderVideoArea(shot, si, shotHasVideo, isGeneratingVideo)}
                            </div>
                        </div>

                        <!-- Divider between Row 1 and Row 2 -->
                        <hr id="shot-divider-${si}" style="border:none;border-top:1px solid var(--border);margin:12px 0;">

                        <!-- Row 2: Assets vertical scroll -->
                        <div id="shot-assets-${si}" style="max-height:40vh;overflow-y:auto;padding:8px 0 0;display:flex;flex-direction:column;gap:8px;">
                            ${renderAssetsRow(shot.assets)}
                        </div>
                    </div>
                </div>`;
            });

            h += '</div>';
            area.innerHTML = h;
        } else {
            area.innerHTML = `
                <div style="text-align:center;padding:40px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">🎬</div>
                    <p>该章节暂无分镜数据</p>
                    <p style="font-size:12px;color:var(--text3);margin-top:8px;">
                        <button class="btn btn-primary btn-sm" onclick="showShotAssetExtractionModal()">🔄 提取分镜和资产</button>
                    </p>
                </div>`;
        }
    }

// ============ Row 2: Assets vertical scroll (one asset per row) ============
    function renderAssetsRow(shotAssets) {
        if (!shotAssets || shotAssets.length === 0) {
            return `
                <div style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text3);padding:20px;min-width:200px;">
                    <span style="font-size:24px;">📦</span>
                    <div style="font-size:12px;margin-left:8px;">暂无绑定资产</div>
                </div>`;
        }

        const typeIcons = { 'Actor': '👤', 'Scene': '🏞️', 'Bgm': '🎵', 'Prop': '📦', 'VoiceVoice': '🎤' };
        const typeNames = { 'Actor': '角色', 'Scene': '场景', 'Bgm': 'BGM', 'Prop': '道具', 'VoiceVoice': '音色' };

        return shotAssets.map(asset => {
            const icon = typeIcons[asset.assetType] || '📄';
            const typeName = typeNames[asset.assetType] || asset.assetType || '未知';
            const role = asset.role ? `<span style="background:var(--primary);color:white;padding:1px 6px;border-radius:3px;font-size:9px;margin-left:4px;">${asset.role}</span>` : '';
            const imgSrc = asset.resourceFilePath || asset.ResourceFilePath || '';
            const description = asset.description || '';
            const imgHtml = imgSrc
                ? `<img src="${imgSrc}" style="width:120px;height:68px;object-fit:cover;border-radius:4px;flex-shrink:0;" alt="${asset.name || asset.Name || ''}" onclick="showImagePreview('${imgSrc}')" title="点击放大">`
                : `<div style="width:120px;height:68px;background:var(--surface2);border:1px dashed var(--border);border-radius:4px;display:flex;align-items:center;justify-content:center;color:var(--text3);font-size:11px;flex-shrink:0;">无图片</div>`;

            return `
                <div style="background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:8px;display:flex;gap:12px;align-items:center;">
                    <!-- Left: Image -->
                    <div style="flex:0 0 120px;">${imgHtml}</div>
                    <!-- Middle: Description -->
                    <div style="flex:1;min-width:0;padding-right:8px;">
                        <div style="display:flex;align-items:center;gap:6px;margin-bottom:4px;">
                            <span style="font-size:16px;">${icon}</span>
                            <span style="font-weight:600;font-size:13px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${asset.name || asset.Name || ''}</span>
                            ${role}
                            <span style="color:var(--text3);font-size:11px;">(${typeName})</span>
                        </div>
                        ${description ? `<div style="font-size:12px;color:var(--text2);line-height:1.5;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;">${description}</div>` : ''}
                    </div>
                    <!-- Right: Other info -->
                    <div style="flex:0 0 200px;text-align:right;color:var(--text3);font-size:11px;display:flex;flex-direction:column;align-items:flex-end;justify-content:center;gap:2px;">
                        ${asset.duration ? `<span>⏱️ ${asset.duration}s</span>` : ''}
                        ${asset.assetType === 'Bgm' ? '<span>🎵 背景音乐</span>' : ''}
                        ${asset.assetType === 'VoiceVoice' ? '<span>🎤 音色</span>' : ''}
                    </div>
                </div>`;
        }).join('');
    }

    // ============ Video Area ============
    function renderVideoArea(shot, shotIdx, hasVideo, isGenerating) {
        return `
            <div style="flex:1;display:flex;flex-direction:column;min-width:0;">
                <!-- Video content - 16:9 aspect ratio via aspect-ratio CSS, limited max-height -->
                <div style="position:relative;background:#000;border-radius:8px;overflow:hidden;aspect-ratio:16/9;max-height:220px;width:100%;">
                    ${hasVideo && shot.videoUrl
                        ? `<video src="${shot.videoUrl}" controls style="width:100%;height:100%;object-fit:contain;background:#000;"></video>
                           <div style="position:absolute;top:8px;right:8px;display:flex;gap:6px;">
                               <button class="btn btn-ghost btn-xs" onclick="showImagePreview('${shot.videoUrl}')" style="background:rgba(0,0,0,0.7);color:#fff;border:none;">🔍 全屏</button>
                               <button class="btn btn-ghost btn-xs" onclick="regenerateShotVideo(${shotIdx})" style="background:rgba(0,0,0,0.7);color:#fff;border:none;">🔄 重新生成</button>
                           </div>`
                        : isGenerating
                            ? `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);">
                                   <span class="spinner" style="width:32px;height:32px;"></span>
                                   <div style="font-size:13px;font-weight:500;">视频生成中...</div>
                                   <div style="font-size:11px;color:var(--text3);">请耐心等待，通常需要 1-3 分钟</div>
                               </div>`
                            : `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);cursor:pointer;" onclick="generateShotVideo(${shotIdx})" title="点击生成视频">
                                   <span style="font-size:40px;">🎬</span>
                                   <div style="font-size:14px;font-weight:600;">点击生成视频</div>
                                   <div style="font-size:11px;color:var(--text3);">基于所有帧描述生成分镜视频</div>
                               </div>`
                    }
                </div>
            </div>`;
    }

    // ============ Frame Card (Vertical Row Layout) ============
    function renderFrameCard(frame, shotIdx, frameIdx, isFirstFrame, shotAssets) {
        if (!frame) return '';

        const frameType = frame.frameType || (frameIdx === 0 ? 'First' : 'Last');
        const typeLabel = frameType === 'First' ? '首帧' : frameType === 'Last' ? '末帧' : '中帧';
        const typeColor = frameType === 'First' ? 'var(--info)' : frameType === 'Last' ? 'var(--danger)' : 'var(--ok)';
        const hasImage = frame.hasImage && frame.imagePath;
        const isGenerating = frame.generating;
        const frameId = `frame-${shotIdx}-${frameIdx}`;

        const imageHtml = hasImage
            ? `<img src="${frame.imagePath}" style="width:100%;height:100%;object-fit:cover;cursor:zoom-in;" onclick="showImagePreview('${frame.imagePath}')" title="点击放大">`
            : `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--text3);cursor:pointer;" onclick="generateFrameImage(${shotIdx}, ${frameIdx})" title="点击生成此帧">
                    <span style="font-size:28px;">📷</span>
                    <span style="font-size:12px;font-weight:500;">点击生成</span>
               </div>`;

        const description = frame.description || '暂无描述';
        const isLongDesc = description.length > 120;

        return `
            <div class="frame-card" id="${frameId}" style="background:var(--surface);border:1px solid var(--border);border-radius:8px;overflow:hidden;display:flex;gap:12px;min-height:120px;">
                <!-- Left: Image (16:9, fixed width ~213px for 120px height) -->
                <div style="flex:0 0 213px;min-width:213px;position:relative;background:var(--surface2);overflow:hidden;display:flex;align-items:center;justify-content:center;">
                    <div style="width:100%;height:120px;position:relative;">
                        ${imageHtml}
                        ${isGenerating ? `<div style="position:absolute;inset:0;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;color:white;font-size:12px;"><span class="spinner-inline" style="margin-right:6px;"></span>生成中...</div>` : ''}
                        <div style="position:absolute;top:6px;left:6px;background:${typeColor};color:white;padding:2px 8px;border-radius:3px;font-size:10px;font-weight:600;">${typeLabel}</div>
                    </div>
                </div>

                <!-- Middle: Description (flexible) -->
                <div style="flex:1;min-width:0;padding:12px 16px;display:flex;flex-direction:column;justify-content:center;position:relative;">
                    <div class="frame-desc" data-full="${description.replace(/"/g, '"')}" data-collapsed="${isLongDesc}" style="font-size:13px;color:var(--text);font-weight:500;line-height:1.6;${isLongDesc ? 'display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;' : ''}">${description}</div>
                    ${isLongDesc ? `<button class="frame-toggle" onclick="toggleFrameDesc('${frameId}')" style="position:absolute;top:8px;right:8px;width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:12px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;" title="展开/收起" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>` : ''}
                </div>

                <!-- Right: Time info (NOT including assets) -->
                <div style="flex:0 0 160px;min-width:160px;padding:12px 16px;display:flex;flex-direction:column;justify-content:center;align-items:flex-end;gap:6px;font-size:11px;color:var(--text3);">
                    ${frame.startTime !== undefined && frame.startTime !== null ? `<span>🕐 ${frame.startTime}s</span>` : ''}
                    ${frame.duration ? `<span>⏱️ ${frame.duration}s</span>` : ''}
                    <span>#${frame.order ?? frameIdx}</span>
                </div>
            </div>`;
    }

    // ============ Video Player Refresh ============
    function playShotVideo(shotIdx) {
        const state = window.shotState[window.currentChapterIdx];
        if (!state || !state.shots[shotIdx]) return;
        const shot = state.shots[shotIdx];
        const area = document.getElementById('videoArea_' + shotIdx);
        if (!area) return;
        area.innerHTML = renderVideoArea(shot, shotIdx, shot.hasVideo && !!shot.videoUrl, shot.generatingVideo);
    }

    function regenerateShotVideo(shotIdx) {
        generateShotVideo(shotIdx);
    }

    // Toggle Frame Description expand/collapse (using data-collapsed attribute)
    function toggleFrameDesc(frameId) {
        const card = document.getElementById(frameId);
        if (!card) return;
        const descEl = card.querySelector('.frame-desc');
        const toggleBtn = card.querySelector('.frame-toggle');
        if (!descEl || !toggleBtn) return;

        const isCollapsed = descEl.getAttribute('data-collapsed') === 'true';
        if (isCollapsed) {
            descEl.style.display = 'block';
            descEl.style.webkitLineClamp = 'unset';
            descEl.style.overflow = 'visible';
            descEl.setAttribute('data-collapsed', 'false');
            toggleBtn.textContent = '▲';
        } else {
            descEl.style.display = '-webkit-box';
            descEl.style.webkitLineClamp = '3';
            descEl.style.webkitBoxOrient = 'vertical';
            descEl.style.overflow = 'hidden';
            descEl.setAttribute('data-collapsed', 'true');
            toggleBtn.textContent = '▼';
        }
    }

    // ============ Shot Frames Toggle ============
    function toggleShotFrames(shotIdx) {
        const contentWrapper = document.getElementById(`shot-content-${shotIdx}`);
        const btn = document.querySelector(`#shot-${shotIdx} .shot-toggle`);
        if (!contentWrapper || !btn) return;

        const isCollapsed = contentWrapper.style.display === 'none';
        if (isCollapsed) {
            contentWrapper.style.display = 'block';
            btn.textContent = '▼';
            btn.title = '收起帧';
        } else {
            contentWrapper.style.display = 'none';
            btn.textContent = '▶';
            btn.title = '展开帧';
        }
    }

    function renderContentTab() {
        const area = document.getElementById('contentArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = `
                <div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">📖</div>
                    <p>暂无章节内容</p>
                </div>`;
            return;
        }

        const ch = window.chapters[window.currentChapterIdx];
        area.innerHTML = `
            <div style="background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:24px;">
                <h3 style="font-size:18px;font-weight:700;margin-bottom:4px;">${ch.chapterNumber} — ${ch.chapterName}</h3>
                <div class="content-body" style="margin-top:16px;">${formatMarkdown(ch.content || '')}</div>
            </div>`;
    }

    // Toggle Shot Description expand/collapse (using data-collapsed attribute)
    function toggleShotDesc(shotId) {
        const descEl = document.querySelector(`#${shotId} .shot-desc`);
        const toggleBtn = document.querySelector(`#${shotId} .shot-desc-toggle`);
        if (!descEl || !toggleBtn) return;

        const isCollapsed = descEl.getAttribute('data-collapsed') === 'true';
        if (isCollapsed) {
            descEl.style.display = 'block';
            descEl.style.webkitLineClamp = 'unset';
            descEl.style.overflow = 'visible';
            descEl.setAttribute('data-collapsed', 'false');
            toggleBtn.textContent = '▲';
        } else {
            descEl.style.display = '-webkit-box';
            descEl.style.webkitLineClamp = '3';
            descEl.style.webkitBoxOrient = 'vertical';
            descEl.style.overflow = 'hidden';
            descEl.setAttribute('data-collapsed', 'true');
            toggleBtn.textContent = '▼';
        }
    }

    // Expose
    window.renderShotsTab = renderShotsTab;
    window.renderFrameCard = renderFrameCard;
    window.playShotVideo = playShotVideo;
    window.regenerateShotVideo = regenerateShotVideo;
    window.renderContentTab = renderContentTab;
    window.toggleFrameDesc = toggleFrameDesc;
    window.toggleShotFrames = toggleShotFrames;
    window.toggleShotDesc = toggleShotDesc;

})();