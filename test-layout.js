const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false, slowMo: 500 });
    const context = await browser.newContext();
    const page = await context.newPage();

    page.on('console', msg => console.log('[BROWSER]', msg.type(), msg.text()));
    page.on('pageerror', err => console.log('[PAGE ERROR]', err.message));

    console.log('1. Open Production page...');
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });
    await page.waitForSelector('#epList .episode-item', { timeout: 10000 });

    // Click Chapter 1 (has data)
    console.log('2. Click Chapter 1...');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);

    // Check layout structure
    console.log('3. Check Row 1: Video + Frames horizontal...');
    const row1 = await page.locator('#storyboardArea > div > div:first-child').evaluate(el => {
        const style = window.getComputedStyle(el);
        return {
            display: style.display,
            flexDirection: style.flexDirection,
            height: style.height,
            gap: style.gap
        };
    });
    console.log('   Row 1 container:', JSON.stringify(row1));

    // Check video area
    const videoArea = await page.locator('#storyboardArea > div > div:first-child > div:first-child').evaluate(el => {
        const style = window.getComputedStyle(el);
        return {
            width: style.width,
            flex: style.flex,
            minWidth: style.minWidth,
            maxWidth: style.maxWidth
        };
    });
    console.log('   Video container:', JSON.stringify(videoArea));

    // Check frames scroll area
    const framesArea = await page.locator('#storyboardArea > div > div:first-child > div:nth-child(2)').evaluate(el => {
        const style = window.getComputedStyle(el);
        return {
            flexDirection: style.flexDirection,
            overflowX: style.overflowX,
            flex: style.flex,
            minWidth: style.minWidth
        };
    });
    console.log('   Frames scroll area:', JSON.stringify(framesArea));

    // Check individual frames in horizontal scroll
    const frameCards = await page.locator('#storyboardArea .frame-card').evaluateAll(cards => 
        cards.map(c => ({
            width: window.getComputedStyle(c).width,
            minWidth: window.getComputedStyle(c).minWidth,
            flex: window.getComputedStyle(c).flex
        }))
    );
    console.log('   Frame cards:', frameCards.length, 'cards');
    frameCards.forEach((fc, i) => console.log(`     Frame ${i}:`, fc));

    // Check Row 2: Assets horizontal scroll
    console.log('4. Check Row 2: Assets horizontal scroll...');
    const row2 = await page.locator('#storyboardArea > div > div:nth-child(2)').evaluate(el => {
        const style = window.getComputedStyle(el);
        return {
            display: style.display,
            overflowX: style.overflowX,
            gap: style.gap,
            flexDirection: style.flexDirection
        };
    });
    console.log('   Row 2 container:', JSON.stringify(row2));

    // Check asset cards
    const assetCards = await page.locator('#storyboardArea .bind-asset-chk, #storyboardArea div[style*="flex:0 0 220px"]').count();
    console.log('   Asset cards count:', assetCards);

    // Take screenshot for visual verification
    await page.screenshot({ path: 'layout-check.png', fullPage: true });
    console.log('5. Screenshot saved: layout-check.png');

    await browser.close();
    console.log('\n✅ Layout check completed!');
})();