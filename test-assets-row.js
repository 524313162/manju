const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForFunction(() => document.querySelectorAll('#epList .episode-item').length > 0, { timeout: 30000 });
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);
    
    const html = await page.evaluate(() => {
        const area = document.getElementById('storyboardArea');
        const shotItem = area.querySelector('.shot-item');
        const contentDiv = shotItem.querySelector('div[style*="padding:12px"]');
        return contentDiv.innerHTML;
    });
    
    // Find the assets row
    const assetsIdx = html.indexOf('display:flex;gap:8px;overflow-x:auto;padding:8px 0 0');
    console.log('Assets row index:', assetsIdx);
    if (assetsIdx > 0) {
        console.log('Assets row HTML (first 2000 chars):');
        console.log(html.substring(assetsIdx, assetsIdx + 2000));
    } else {
        console.log('Assets row NOT FOUND');
        // Look for any overflow-x:auto
        const overflowIdx = html.indexOf('overflow-x:auto');
        console.log('First overflow-x:auto at:', overflowIdx);
        if (overflowIdx > 0) {
            console.log(html.substring(overflowIdx, overflowIdx + 500));
        }
    }
    
    await browser.close();
})();