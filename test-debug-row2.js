const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });
    await page.waitForSelector('#epList .episode-item');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);
    
    const row2HTML = await page.evaluate(() => {
        const area = document.getElementById('storyboardArea');
        const shotItem = area.querySelector('.shot-item');
        const columnContainer = shotItem.querySelector('div[style*="flex-direction:column"]');
        const children = columnContainer ? columnContainer.children : [];
        console.log('Column container children count:', children.length);
        for (let i = 0; i < children.length; i++) {
            console.log('Child', i, 'style.display:', window.getComputedStyle(children[i]).display);
            console.log('Child', i, 'style.cssText:', children[i].style.cssText.substring(0, 100));
        }
        return children[1] ? children[1].outerHTML.substring(0, 500) : 'NOT FOUND';
    });
    console.log('Row2 HTML:', row2HTML);
    
    await browser.close();
})();