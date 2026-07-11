const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false, slowMo: 500 });
    const context = await browser.newContext();
    const page = await context.newPage();

    // Capture console errors
    page.on('console', msg => {
        if (msg.type() === 'error') console.log('[BROWSER ERROR]', msg.text());
    });
    page.on('pageerror', err => console.log('[PAGE ERROR]', err.message));

    console.log('1. Open Production page...');
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });

    // Wait for chapters to load in sidebar
    console.log('2. Wait for chapters sidebar...');
    await page.waitForSelector('#epList .episode-item', { timeout: 10000 });

    const chapterCount = await page.locator('#epList .episode-item').count();
    console.log(`   Found ${chapterCount} chapters in sidebar`);

    // Check first chapter is active
    const firstChapter = page.locator('#epList .episode-item').first();
    const isActive = await firstChapter.getAttribute('class');
    console.log(`   First chapter class: ${isActive}`);

    // Check right panel content
    console.log('3. Check right panel content...');
    await page.waitForTimeout(1000); // Let rendering complete

    const storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea.length} chars`);
    console.log(`   Contains "请先编辑剧本": ${storyboardArea.includes('请先编辑剧本') || storyboardArea.includes('请先编辑剧本添加章节')}`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea.includes('暂无分镜数据')}`);
    console.log(`   Contains "分镜": ${storyboardArea.includes('分镜')}`);
    console.log(`   Contains "生成视频": ${storyboardArea.includes('生成视频')}`);
    console.log(`   Contains "加载": ${storyboardArea.includes('加载')}`);

    // Click Import Shots button
    console.log('4. Click 导入分镜 button...');
    await page.click('button:has-text("导入分镜")');
    await page.waitForTimeout(500);

    const importModal = await page.locator('#importShotsModal').isVisible();
    console.log(`   Import modal visible: ${importModal}`);

    if (importModal) {
        // Close modal
        await page.click('#importShotsModal .modal-close');
        await page.waitForTimeout(300);
        console.log('   Modal closed');
    }

    // Click chapter 2
    console.log('5. Click chapter 2...');
    await page.click('#epList .episode-item:nth-child(2)');
    await page.waitForTimeout(1000);

    const storyboardArea2 = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea2.length} chars`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea2.includes('暂无分镜数据')}`);
    console.log(`   Contains "分镜": ${storyboardArea2.includes('分镜')}`);
    console.log(`   Contains "生成视频": ${storyboardArea2.includes('生成视频')}`);

    // Click chapter 1 again
    console.log('6. Click chapter 1...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(1000);

    const storyboardArea1 = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea1.length} chars`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea1.includes('暂无分镜数据')}`);
    console.log(`   Contains "分镜": ${storyboardArea1.includes('分镜')}`);
    console.log(`   Contains "生成视频": ${storyboardArea1.includes('生成视频')}`);

    // Check JS state
    const jsState = await page.evaluate(() => ({
        chapters: window.chapters.length,
        currentChapterIdx: window.currentChapterIdx,
        currentTab: window.currentTab,
        shotStateKeys: Object.keys(window.shotState),
        shotState0: window.shotState[0]
    }));
    console.log('7. JS State:', JSON.stringify(jsState, null, 2));

    await browser.close();
})();