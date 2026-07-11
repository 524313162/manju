const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false, slowMo: 500 });
    const context = await browser.newContext();
    const page = await context.newPage();

    page.on('console', msg => {
        console.log('[BROWSER CONSOLE]', msg.type(), msg.text());
    });
    page.on('pageerror', err => console.log('[PAGE ERROR]', err.message));

    console.log('1. Open Production page...');
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });

    await page.waitForSelector('#epList .episode-item', { timeout: 10000 });
    console.log('2. Sidebar loaded');

    // Click Chapter 2 (which has no shots)
    console.log('3. Click Chapter 2...');
    await page.click('#epList .episode-item:nth-child(2)');
    await page.waitForTimeout(2000);

    // Check if "暂无分镜数据" is shown
    let storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   Contains "暂无分镜数据": ${storyboardArea.includes('暂无分镜数据')}`);

    // Click "提取分镜和资产" button
    console.log('4. Click "提取分镜和资产" button...');
    await page.click('button:has-text("提取分镜和资产")');
    await page.waitForTimeout(1000);

    // Check projectId
    const projectIdCheck = await page.evaluate(() => window.projectId);
    console.log(`   window.projectId: ${projectIdCheck}`);

    // Check if modal opened
    const modalVisible = await page.locator('#shotAssetExtractModal').isVisible();
    console.log(`   Modal visible: ${modalVisible}`);

    if (modalVisible) {
        // Wait for providers to load
        await page.waitForTimeout(2000);
        
        // Check providers dropdown
        const providerOptions = await page.locator('#shotAssetExtractProvider option').allTextContents();
        console.log('   Available providers:', providerOptions.join(', '));

        // Select NVIDIA NIM - Llama 3.1 70B
        console.log('5. Select NVIDIA NIM - Llama 3.1 70B...');
        await page.selectOption('#shotAssetExtractProvider', { label: 'NVIDIA NIM - Llama 3.1 70B [meta/llama-3.1-70b-instruct]' });
        await page.waitForTimeout(500);

        // Check if prompt loaded
        const promptValue = await page.locator('#shotAssetExtractPrompt').inputValue();
        console.log(`   Prompt loaded: ${promptValue.length > 50 ? 'YES (' + promptValue.length + ' chars)' : 'NO - ' + promptValue}`);

        // Click Extract button
        console.log('6. Click "提取" button...');
        
        // Handle the result
        page.on('dialog', async dialog => {
            console.log(`   Dialog: ${dialog.message()}`);
            await dialog.accept();
        });

        await page.click('#shotAssetExtractBtn');
        
        // Wait for result (could take a while for AI)
        console.log('   Waiting for extraction result (60s timeout)...');
        try {
            await page.waitForFunction(() => {
                const resultDiv = document.getElementById('shotAssetExtractResult');
                return resultDiv && (resultDiv.innerHTML.includes('提取完成') || 
                                     resultDiv.innerHTML.includes('提取失败') ||
                                     resultDiv.innerHTML.includes('错误') ||
                                     resultDiv.innerHTML.includes('保存'));
            }, { timeout: 120000 });
            
            const resultHtml = await page.locator('#shotAssetExtractResult').innerHTML();
            console.log('   Result received!');
            console.log('   Contains "提取完成":', resultHtml.includes('提取完成'));
            console.log('   Contains "提取失败":', resultHtml.includes('提取失败'));
            console.log('   Contains "错误":', resultHtml.includes('错误'));
            console.log('   Contains "保存":', resultHtml.includes('保存'));
            
            if (resultHtml.includes('提取完成') || resultHtml.includes('保存')) {
                console.log('   ✅ Extraction SUCCESS!');
                
                // Check if preview modal appears
                await page.waitForTimeout(2000);
                const previewVisible = await page.locator('#extractionPreviewModal').isVisible();
                console.log(`   Preview modal visible: ${previewVisible}`);
                
                if (previewVisible) {
                    // Click confirm save
                    console.log('7. Click "确认保存"...');
                    await page.click('#confirmSaveBtn');
                    await page.waitForTimeout(3000);
                    console.log('   Save completed!');
                }
            } else {
                console.log('   ❌ Extraction FAILED');
                console.log('   Result HTML:', resultHtml.substring(0, 500));
            }
        } catch (e) {
            console.log('   Timeout waiting for result:', e.message);
            const resultHtml = await page.locator('#shotAssetExtractResult').innerHTML();
            console.log('   Current result HTML:', resultHtml.substring(0, 500));
        }
    }

    await browser.close();
    console.log('\n✅ Full extraction flow test completed!');
})();