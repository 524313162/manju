const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle' });
    await page.waitForSelector('#epList .episode-item');
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);
    
    const structure = await page.evaluate(() => {
        const area = document.getElementById('storyboardArea');
        const shotItem = area.querySelector('.shot-item');
        console.log('Shot item direct children count:', shotItem.children.length);
        
        for (let i = 0; i < shotItem.children.length; i++) {
            const child = shotItem.children[i];
            console.log(`Direct child ${i}:`, {
                className: child.className,
                styleDisplay: window.getComputedStyle(child).display,
                styleCssText: child.style.cssText.substring(0, 120)
            });
        }
        
        // Find the flex-column container (3rd child)
        const columnContainer = shotItem.children[2];
        if (columnContainer) {
            console.log('\nColumn container children count:', columnContainer.children.length);
            for (let i = 0; i < columnContainer.children.length; i++) {
                const child = columnContainer.children[i];
                console.log(`Column child ${i}:`, {
                    styleDisplay: window.getComputedStyle(child).display,
                    styleCssText: child.style.cssText.substring(0, 150),
                    tagName: child.tagName
                });
            }
        }
    });
    
    await browser.close();
})();