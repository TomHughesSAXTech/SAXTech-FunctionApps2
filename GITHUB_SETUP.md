# GitHub Setup Instructions

## Step 1: Create GitHub Repository

1. Go to https://github.com and sign in
2. Click "New repository"
3. Name it: `SAXTech-FunctionApps2`
4. Set to Public or Private (your choice)
5. **Don't** initialize with README (we already have one)
6. Click "Create repository"

## Step 2: Push Your Code

In your terminal, run these commands from the SAXTech-FunctionApps2 directory:

```bash
# Add the GitHub remote (replace YOUR-USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR-USERNAME/SAXTech-FunctionApps2.git

# Push to GitHub
git branch -M main
git push -u origin main
```

## Step 3: Add the Publish Profile Secret

1. In your GitHub repository, go to **Settings** → **Secrets and variables** → **Actions**
2. Click **"New repository secret"**
3. Name: `AZURE_FUNCTIONAPP_PUBLISH_PROFILE`
4. Value: Copy and paste the content below (it's your Azure Function App publish profile):

```xml
<publishData><publishProfile profileName="SAXTech-FunctionApps2 - Web Deploy" publishMethod="MSDeploy" publishUrl="saxtech-functionapps2.scm.azurewebsites.net:443" msdeploySite="SAXTech-FunctionApps2" userName="$SAXTech-FunctionApps2" userPWD="REDACTED_PASSWORD" destinationAppUrl="https://saxtech-functionapps2.azurewebsites.net" SQLServerDBConnectionString="" mySQLDBConnectionString="" hostingProviderForumLink="" controlPanelLink="https://portal.azure.com" webSystem="WebSites"><databases /></publishProfile><publishProfile profileName="SAXTech-FunctionApps2 - FTP" publishMethod="FTP" publishUrl="ftps://waws-prod-bn1-285.ftp.azurewebsites.windows.net/site/wwwroot" ftpPassiveMode="True" userName="SAXTech-FunctionApps2\$SAXTech-FunctionApps2" userPWD="REDACTED_PASSWORD" destinationAppUrl="https://saxtech-functionapps2.azurewebsites.net" SQLServerDBConnectionString="" mySQLDBConnectionString="" hostingProviderForumLink="" controlPanelLink="https://portal.azure.com" webSystem="WebSites"><databases /></publishProfile><publishProfile profileName="SAXTech-FunctionApps2 - Zip Deploy" publishMethod="ZipDeploy" publishUrl="saxtech-functionapps2.scm.azurewebsites.net:443" userName="$SAXTech-FunctionApps2" userPWD="REDACTED_PASSWORD" destinationAppUrl="https://saxtech-functionapps2.azurewebsites.net" SQLServerDBConnectionString="" mySQLDBConnectionString="" hostingProviderForumLink="" controlPanelLink="https://portal.azure.com" webSystem="WebSites"><databases /></publishProfile></publishData>
```

**⚠️ IMPORTANT:** I've redacted the passwords above for security. You need to get the actual publish profile with the real passwords.

## Step 4: Get the Real Publish Profile

Run this command to get your actual publish profile with passwords:

```bash
az functionapp deployment list-publishing-profiles --resource-group SAXTech-AI --name SAXTech-FunctionApps2 --xml
```

Copy the entire output and use that as your secret value instead of the redacted version above.

## Step 5: Test the Deployment

1. Once you've set up the secret, go to your GitHub repository
2. Click on **Actions** tab
3. You should see the workflow run automatically
4. Or click **"Run workflow"** to trigger it manually

## Step 6: Verify Function is Working

After successful deployment (usually takes 3-5 minutes):

1. Go to https://portal.azure.com
2. Navigate to SAXTech-AI → SAXTech-FunctionApps2 → Functions
3. You should see "ConvertDocument" function listed
4. Your API endpoint will be: `https://saxtech-functionapps2.azurewebsites.net/api/ConvertDocument`

## That's it! 

Once set up, any push to the main branch will automatically build and deploy your Function App.
