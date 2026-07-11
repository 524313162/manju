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

    // Click chapter 1 (which has shots)
    console.log('3. Click chapter 1...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000); // Wait for shots to load

    // Check shots are loaded
    const storyboardAreaBefore = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardAreaBefore.length} chars`);
    console.log(`   Contains "SHOT 1": ${storyboardAreaBefore.includes('SHOT 1')}`);
    console.log(`   Contains "生成视频": ${storyboardAreaBefore.includes('生成视频')}`);
    console.log(`   Contains "分帧模板": ${storyboardAreaBefore.includes('分帧模板')}`);

    // Check Clear Shots button exists
    console.log('4. Check Clear Shots button...');
    const clearBtn = await page.locator('button:has-text("清空分镜")').count();
    console.log(`   Clear Shots button count: ${clearBtn}`);

    if (clearBtn > 0) {
        console.log('5. Click Clear Shots button...');
        
        // Handle confirm dialog
        page.on('dialog', async dialog => {
            console.log(`   Confirm dialog: ${dialog.message()}`);
            await dialog.accept();
        });
        
        await page.click('button:has-text("清空分镜")');
        await page.waitForTimeout(2000); // Wait for API call and reload

        // Check shots are cleared
        const storyboardAreaAfter = await page.locator('#storyboardArea').innerHTML();
        console.log(`   storyboardArea length after clear: ${storyboardAreaAfter.length} chars`);
        console.log(`   Contains "暂无分镜数据": ${storyboardAreaAfter.includes('暂无分镜数据')}`);
        console.log(`   Contains "SHOT 1": ${storyboardAreaAfter.includes('SHOT 1')}`);
        
        // Verify sidebar shows 0 shots
        const episodeItem = await page.locator('#epList .episode-item.active').innerHTML();
        console.log(`   Active chapter sidebar shows: ${episodeItem.includes('未提取分镜') || episodeItem.includes('0 分镜')}`);
    }

    // Check JS state
    const jsState = await page.evaluate(() => ({
        chapters: window.chapters.length,
        currentChapterIdx: window.currentChapterIdx,
        currentTab: window.currentTab,
        shotStateKeys: Object.keys(window.shotState),
        shotState0: window.shotState[0]
    }));
    console.log('6. JS State after clear:', JSON.stringify(jsState, null, 2));

    await browser.close();
    console.log('\n✅ Clear Shots test completed!');
})();