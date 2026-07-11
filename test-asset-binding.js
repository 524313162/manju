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

    // Click Chapter 1 (has shots)
    console.log('3. Click Chapter 1...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);

    // Check shots are displayed
    let storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea.length} chars`);
    console.log(`   Contains SHOT 1: ${storyboardArea.includes('SHOT 1')}`);

    // Click "绑定资产" button for Shot 1
    console.log('4. Click "绑定资产" button for Shot 1...');
    await page.click('button:has-text("绑定资产")');
    await page.waitForTimeout(1000);

    // Check modal is visible
    const modalVisible = await page.locator('#bindAssetModal').isVisible();
    console.log(`   Modal visible: ${modalVisible}`);

    if (modalVisible) {
        // Check current assets display
        const currentText = await page.locator('#bindAssetCurrent').textContent();
        console.log(`   Current assets display: "${currentText}"`);

        // Check if assets are pre-checked
        const checkedCount = await page.locator('#bindAssetList .bind-asset-chk:checked').count();
        console.log(`   Pre-checked assets count: ${checkedCount}`);

        // Check if roles are displayed
        const roleLabels = await page.locator('#bindAssetList label:has-text("主角")').count();
        const sceneLabels = await page.locator('#bindAssetList label:has-text("场景")').count();
        const propLabels = await page.locator('#bindAssetList label:has-text("道具")').count();
        console.log(`   Labels with "主角": ${roleLabels}`);
        console.log(`   Labels with "场景": ${sceneLabels}`);
        console.log(`   Labels with "道具": ${propLabels}`);

        // Check specific asset names are present
        const princessName = await page.locator('#bindAssetList label:has-text("太平公主")').count();
        const lixuanName = await page.locator('#bindAssetList label:has-text("李轩(男一号)")').count();
        const palaceName = await page.locator('#bindAssetList label:has-text("公主府")').count();
        const soupName = await page.locator('#bindAssetList label:has-text("醒酒汤")').count();
        console.log(`   "太平公主" present: ${princessName > 0}`);
        console.log(`   "李轩(男一号)" present: ${lixuanName > 0}`);
        console.log(`   "公主府" present: ${palaceName > 0}`);
        console.log(`   "醒酒汤" present: ${soupName > 0}`);

        // Close modal
        await page.click('#bindAssetModal .modal-close');
        await page.waitForTimeout(500);
    }

    // Test Shot 2 binding
    console.log('5. Click "绑定资产" button for Shot 2...');
    await page.click('button:has-text("绑定资产"):nth-of-type(2)');
    await page.waitForTimeout(1000);

    const modalVisible2 = await page.locator('#bindAssetModal').isVisible();
    console.log(`   Modal visible: ${modalVisible2}`);

    if (modalVisible2) {
        const currentText2 = await page.locator('#bindAssetCurrent').textContent();
        console.log(`   Current assets display: "${currentText2}"`);

        const checkedCount2 = await page.locator('#bindAssetList .bind-asset-chk:checked').count();
        console.log(`   Pre-checked assets count: ${checkedCount2}`);

        const princessName2 = await page.locator('#bindAssetList label:has-text("太平公主")').count();
        const lixuanName2 = await page.locator('#bindAssetList label:has-text("李轩(男一号)")').count();
        const palaceName2 = await page.locator('#bindAssetList label:has-text("公主府")').count();
        console.log(`   "太平公主" present: ${princessName2 > 0}`);
        console.log(`   "李轩(男一号)" present: ${lixuanName2 > 0}`);
        console.log(`   "公主府" present: ${palaceName2 > 0}`);

        await page.click('#bindAssetModal .modal-close');
        await page.waitForTimeout(500);
    }

    // Test video area toggle
    console.log('6. Test video area toggle...');
    const videoToggleBtn = await page.locator('button:has-text("视频预览")').first();
    if (await videoToggleBtn.isVisible()) {
        await videoToggleBtn.click();
        await page.waitForTimeout(500);
        
        const videoContent = await page.locator('#videoContent_0').isVisible();
        console.log(`   Video content visible after toggle: ${videoContent}`);
        
        // Toggle again
        await videoToggleBtn.click();
        await page.waitForTimeout(500);
        
        const videoContent2 = await page.locator('#videoContent_0').isVisible();
        console.log(`   Video content visible after second toggle: ${videoContent2}`);
    }

    // Test frame expansion
    console.log('7. Test frame expansion...');
    const expandBtn = await page.locator('button:has-text("展开全部帧")').first();
    if (await expandBtn.isVisible()) {
        await expandBtn.click();
        await page.waitForTimeout(500);
        
        const framesVisible = await page.locator('#frames_0').isVisible();
        console.log(`   Frames visible after expand: ${framesVisible}`);
        
        // Collapse
        await page.click('button:has-text("收起帧")');
        await page.waitForTimeout(500);
        
        const framesVisible2 = await page.locator('#frames_0').isVisible();
        console.log(`   Frames visible after collapse: ${framesVisible2}`);
    }

    await browser.close();
    console.log('\n✅ All tests completed!');
})();