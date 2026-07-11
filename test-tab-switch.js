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

    // Click chapter 1 (should have shots from seed data)
    console.log('3. Click chapter 1...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);

    // Check if shots are displayed
    let storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea.length} chars`);
    console.log(`   Contains "SHOT 1": ${storyboardArea.includes('SHOT 1')}`);
    console.log(`   Contains "生成视频": ${storyboardArea.includes('生成视频')}`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea.includes('暂无分镜数据')}`);

    // Switch to content tab
    console.log('4. Switch to "内容" tab...');
    await page.click('.prod-tab[data-tab="content"]');
    await page.waitForTimeout(500);

    // Check content tab
    let contentArea = await page.locator('#contentArea').innerHTML();
    console.log(`   Content tab loaded: ${contentArea.length > 100 ? 'YES' : 'NO'}`);

    // Switch back to shots tab
    console.log('5. Switch back to "分镜" tab...');
    await page.click('.prod-tab[data-tab="shots"]');
    await page.waitForTimeout(1000);

    // Check shots tab again
    storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea.length} chars`);
    console.log(`   Contains "SHOT 1": ${storyboardArea.includes('SHOT 1')}`);
    console.log(`   Contains "生成视频": ${storyboardArea.includes('生成视频')}`);
    console.log(`   Contains "正在加载": ${storyboardArea.includes('正在加载')}`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea.includes('暂无分镜数据')}`);

    // Switch to content again
    console.log('6. Switch to "内容" tab again...');
    await page.click('.prod-tab[data-tab="content"]');
    await page.waitForTimeout(500);

    // Switch back to shots
    console.log('7. Switch back to "分镜" tab again...');
    await page.click('.prod-tab[data-tab="shots"]');
    await page.waitForTimeout(1000);

    storyboardArea = await page.locator('#storyboardArea').innerHTML();
    console.log(`   storyboardArea length: ${storyboardArea.length} chars`);
    console.log(`   Contains "SHOT 1": ${storyboardArea.includes('SHOT 1')}`);
    console.log(`   Contains "正在加载": ${storyboardArea.includes('正在加载')}`);
    console.log(`   Contains "暂无分镜数据": ${storyboardArea.includes('暂无分镜数据')}`);

    // Final JS state
    const jsState = await page.evaluate(() => ({
        shotState0: window.shotState[0]
    }));
    console.log('\n8. Final JS State:', JSON.stringify(jsState, null, 2));

    await browser.close();
    console.log('\n✅ Tab switching test completed!');
})();