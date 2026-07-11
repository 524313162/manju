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
        if (!shotItem) return 'No shot-item';
        
        const contentDiv = shotItem.querySelector('div[style*="padding:12px"]');
        if (!contentDiv) return 'No content div';
        
        return contentDiv.innerHTML;
    });
    
    console.log('Content div HTML (first 5000 chars):');
    console.log(html.substring(0, 5000));
    
    await browser.close();
})();