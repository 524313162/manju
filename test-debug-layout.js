const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false, slowMo: 300 });
    const page = await browser.newPage();
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });
    await page.waitForSelector('#epList .episode-item');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);
    
    await page.screenshot({ path: 'debug-layout.png', fullPage: true });
    console.log('Screenshot saved: debug-layout.png');
    
    const styles = await page.evaluate(() => {
        const area = document.getElementById('storyboardArea');
        const shotItem = area.querySelector('.shot-item');
        const row1 = shotItem.querySelector('div[style*="height:250px"]');
        const videoContainer = row1?.querySelector('div[style*="flex:0 0 50%"]');
        const framesContainer = row1?.querySelector('div[style*="flex:1;min-width:0"]');
        const row2 = shotItem.querySelector('div[style*="overflow-x:auto"][style*="padding:4px 0"]');
        
        return {
            shotItemDisplay: window.getComputedStyle(shotItem).display,
            row1Display: window.getComputedStyle(row1).display,
            row1Height: window.getComputedStyle(row1).height,
            videoContainerFlex: window.getComputedStyle(videoContainer).flex,
            videoContainerWidth: window.getComputedStyle(videoContainer).width,
            framesContainerFlex: window.getComputedStyle(framesContainer).flex,
            framesContainerOverflowX: window.getComputedStyle(framesContainer).overflowX,
            framesContainerWidth: window.getComputedStyle(framesContainer).width,
            row2Display: window.getComputedStyle(row2).display,
            row2OverflowX: window.getComputedStyle(row2).overflowX,
        };
    });
    console.log('Computed styles:', JSON.stringify(styles, null, 2));
    
    await browser.close();
})();