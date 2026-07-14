const { chromium } = require("playwright");
(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  page.on("console", msg => console.log("CONSOLE:", msg.type(), msg.text()));
  page.on("pageerror", err => console.log("PAGE_ERROR:", err.message));

  await page.goto("http://localhost:5678/Production?projectId=1", { waitUntil: "networkidle", timeout: 15000 });

  await page.waitForSelector("#epList .ep-item", { timeout: 10000 }).catch(() => console.log("no ep items"));

  const epItems = await page.$$("#epList .ep-item");
  if (epItems.length > 0) {
    await epItems[0].click();
    await page.waitForTimeout(3000);
  }

  await page.waitForSelector(".shot-item", { timeout: 10000 }).catch(() => console.log("no shot items"));
  await page.waitForTimeout(1000);

  const el = await page.$("#sagAssetId");
  console.log("sagAssetId exists:", !!el);

  const assetImg = await page.$(".shot-item [onclick*='showSingleAssetGenModalFromFrame']");
  if (assetImg) {
    console.log("Found clickable asset element, clicking...");
    await assetImg.click();
    await page.waitForTimeout(1000);
    const modal = await page.$("#singleAssetGenModal.show");
    console.log("Modal visible:", !!modal, "class:", modal ? await modal.getAttribute("class") : "N/A");
    const title = await page.$("#singleAssetGenTitle");
    console.log("Modal title:", title ? await title.textContent() : "N/A");
  } else {
    console.log("No asset image found");
    const html = await page.$eval("#storyboardArea", el => el.innerHTML.substring(0, 1000)).catch(() => "no storyboard area");
    console.log("Storyboard:", html);
  }

  await browser.close();
})();
