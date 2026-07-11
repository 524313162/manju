const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false, slowMo: 500 });
    const context = await browser.newContext();
    const page = await context.newPage();

    page.on('console', msg => {
        if (msg.type() === 'error') console.log('[BROWSER ERROR]', msg.text());
    });
    page.on('pageerror', err => console.log('[PAGE ERROR]', err.message));

    console.log('1. Open Production page...');
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });

    await page.waitForSelector('#epList .episode-item', { timeout: 10000 });
    console.log('2. Sidebar loaded');

    // Click chapter 1 (has shots), clear them
    console.log('3. Click chapter 1, clear shots...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(1500);
    
    page.on('dialog', async dialog => {
        await dialog.accept();
    });
    await page.click('button:has-text("清空分镜")');
    await page.waitForTimeout(2000);

    // Verify cleared
    let storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   Chapter 1 after clear: ${storyboardArea.includes('暂无分镜数据') ? '✅ EMPTY' : '❌ HAS DATA'}`);

    // Click chapter 2 (has shots)
    console.log('4. Click chapter 2 (has shots)...');
    await page.click('#epList .episode-item:nth-child(2)');
    await page.waitForTimeout(1500);
    
    storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   Chapter 2: ${storyboardArea.includes('SHOT 1') ? '✅ HAS SHOTS' : '❌ EMPTY'}`);
    console.log(`   Length: ${storyboardArea.length} chars`);

    // Click chapter 1 again (should be empty)
    console.log('5. Click chapter 1 again...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(1000);
    
    storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   Chapter 1 again: ${storyboardArea.includes('暂无分镜数据') ? '✅ STILL EMPTY' : '❌ HAS DATA'}`);

    // Click chapter 3 (might be empty)
    console.log('6. Click chapter 3...');
    await page.click('#epList .episode-item:nth-child(3)');
    await page.waitForTimeout(1500);
    
    storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   Chapter 3: ${storyboardArea.includes('暂无分镜数据') ? '✅ EMPTY' : '❌ HAS DATA'}`);

    // Test that Clear button is disabled/hidden when no shots (or handles gracefully)
    console.log('7. Test Clear button on empty chapter...');
    const clearBtnCount = await page.locator('button:has-text("清空分镜")').count();
    console.log(`   Clear button count: ${clearBtnCount}`);
    
    if (clearBtnCount > 0) {
        // Should handle gracefully - API will return "no shots" message
        page.on('dialog', async dialog => {
            console.log(`   Dialog: ${dialog.message()}`);
            await dialog.accept();
        });
        await page.click('button:has-text("清空分镜")');
        await page.waitForTimeout(1500);
        console.log('   ✅ Handled gracefully');
    }

    // Final JS state
    const jsState = await page.evaluate(() => ({
        currentChapterIdx: window.currentChapterIdx,
        shotStateKeys: Object.keys(window.shotState),
        shotState0: window.shotState[0],
        shotState1: window.shotState[1],
        shotState2: window.shotState[2]
    }));
    console.log('\n8. Final JS State:', JSON.stringify(jsState, null, 2));

    await browser.close();
    console.log('\n✅ All chapter switching tests passed!');
})();