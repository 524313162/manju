function apiCreateAsset(projectId, assetType, name, description, order, parentId) {
    return fetch('/Assets/Create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId: projectId, assetType: assetType, name: name, description: description, order: order, parentId: parentId || '' })
    }).then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); });
}

function apiEditAsset(id, name, description, order, parentId) {
    return fetch('/Assets/Edit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: id, name: name, description: description, order: order, parentId: parentId })
    }).then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); });
}

function apiDeleteAsset(id) {
    return fetch('/Assets/Delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: id })
    }).then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); });
}

function apiClearAllAssets(projectId) {
    return fetch('/Assets/ClearAll', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId: projectId })
    }).then(function(r) { return r.json(); });
}

function apiListByProject(projectId, type) {
    var url = '/Assets/ListByProject?projectId=' + projectId;
    if (type !== undefined && type !== null) {
        url += '&type=' + type;
    }
    return fetch(url).then(function(r) { return r.json(); });
}

function apiReplaceResource(assetId, projectId, file) {
    var fd = new FormData();
    fd.append('assetid', assetId);
    fd.append('projectid', projectId);
    fd.append('uploadFile', file);
    return fetch('/Assets/ReplaceResource', { method: 'POST', body: fd })
        .then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); });
}

function apiReplaceAudio(assetId, fileUrl) {
    return fetch('/Assets/ReplaceAudio', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assetId: assetId, fileUrl: fileUrl })
    }).then(function(r) { return r.json(); });
}

function apiBulkCreate(projectId, assets) {
    return fetch('/Assets/BulkCreate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId: projectId, assets: assets })
    }).then(function(r) { return r.json(); });
}

function apiGetGenerationTemplates() {
    return fetch('/Assets/GetGenerationTemplates')
        .then(function(r) { return r.json(); })
        .then(function(data) { return data.success ? data.data : {}; });
}

function apiGetImageModels() {
    return fetch('/api/v1/providers/image-models')
        .then(function(r) { return r.json(); })
        .then(function(data) { return data.success ? data.data : []; });
}

function apiGenerateImage(prompt, width, height, modelId) {
    var payload = { prompt: prompt, width: width || 1024, height: height || 768 };
    if (modelId) payload.modelId = modelId;
    return fetch('/api/comfyui/zimage/text-to-image', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    }).then(function(r) { return r.json(); });
}

function apiGenerateBgm(prompt, modelId) {
    var payload = { prompt: prompt };
    if (modelId) payload.modelId = modelId;
    return fetch('/api/comfyui/stable-bgm/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    }).then(function(r) { return r.json(); });
}

function apiGenerateAudioCompose(prompt, modelId) {
    var payload = { prompt: prompt };
    if (modelId) payload.modelId = modelId;
    return fetch('/api/comfyui/ace-music/compose', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    }).then(function(r) { return r.json(); });
}

function apiGetResult(promptId, resultType, workflowType) {
    return fetch('/api/v1/comfyui/result/' + promptId + '/' + resultType + '?workflowType=' + encodeURIComponent(workflowType))
        .then(function(r) { return r.json(); });
}

function apiSaveAssetResource(assetId, isAudio, fileUrl) {
    var endpoint = isAudio ? '/Assets/ReplaceAudio' : '/Assets/ReplaceResource';
    return fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assetId: assetId, fileUrl: fileUrl })
    }).then(function(r) { return r.json(); });
}