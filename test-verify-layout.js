const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: false });
    const page = await browser.newPage();
    await page.goto('http://localhost:5001/Production?projectId=1', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForFunction(() => document.querySelectorAll('#epList .episode-item').length > 0, { timeout: 30000 });
    await page.click('#epList .episode-item:nth-child(1)');
    await page.waitForTimeout(2000);
    
    // Check layout structure
    const structure = await page.evaluate(() => {
        const area = document.getElementById('storyboardArea');
        const shotItem = area.querySelector('.shot-item');
        if (!shotItem) return { error: 'No shot-item' };
        
        // Find the content div (padding:12px)
        const contentDiv = shotItem.querySelector('div[style*="padding:12px"]');
        if (!contentDiv) return { error: 'No content div', contentDiv: contentDiv?.innerHTML?.substring(0, 200) };
        
        const children = contentDiv.children;
        console.log('children count:', children.length);
        for (let i = 0; i < children.length; i++) {
            console.log(`child ${i}:`, children[i].tagName, children[i].style.cssText.substring(0, 100));
        }
        
        const row1 = children[0];
        const row2 = children[1];
        
        if (!row1 || !row2) return { error: 'Missing row1 or row2', childrenCount: children.length };
        
        return {
            row1: {
                display: window.getComputedStyle(row1).display,
                height: window.getComputedStyle(row1).height,
                gap: window.getComputedStyle(row1).gap,
                childCount: row1.children.length,
                child0: row1.children[0] ? { flex: window.getComputedStyle(row1.children[0]).flex, width: window.getComputedStyle(row1.children[0]).width } : null,
                child1: row1.children[1] ? { flex: window.getComputedStyle(row1.children[1]).flex, width: window.getComputedStyle(row1.children[1]).width } : null
            },
            row2: {
                display: window.getComputedStyle(row2).display,
                overflowX: window.getComputedStyle(row2).overflowX,
                gap: window.getComputedStyle(row2).gap,
                childCount: row2.children.length
            },
            frameCards: shotItem.querySelectorAll('.frame-card').length,
            assetCards: row2.querySelectorAll('[style*="flex:0 0 220px"]').length
        };
    });
    console.log('Layout structure:', JSON.stringify(structure, null, 2));
    
    await browser.close();
})();