(function() {
    var script = document.currentScript || document.getElementsByTagName('script')[document.getElementsByTagName('script').length - 1];
    if (script) {
        var dataset = script.dataset;
        if (dataset.projectId !== undefined) projectId = parseInt(dataset.projectId);
        if (dataset.currentType !== undefined) currentType = parseInt(dataset.currentType);
        if (dataset.typeName !== undefined) typeName = dataset.typeName;
    }

    document.addEventListener('DOMContentLoaded', function() {
        setupButtonListeners();
        checkPendingExtractAssets();
    });
})();